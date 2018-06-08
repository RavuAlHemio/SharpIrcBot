using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SharpIrcBot.Collections;
using SharpIrcBot.Events.Irc;

namespace SharpIrcBot.Plugins.Debounce
{
    public class DebouncePlugin : IPlugin, IReloadableConfiguration
    {
        protected IConnectionManager ConnectionManager { get; }
        protected DebounceConfig Config { get; set; }
        protected RegexCache RegexCache { get; set; }
        protected List<JoinQuitEvent> RelevantJoins { get; set; }
        protected Random RNG { get; set; }

        public DebouncePlugin(IConnectionManager connMgr, JObject config)
        {
            ConnectionManager = connMgr;
            Config = new DebounceConfig(config);
            RegexCache = new RegexCache();
            RelevantJoins = new List<JoinQuitEvent>();
            RNG = new Random();

            ConnectionManager.JoinedChannel += HandleUserJoin;
            ConnectionManager.ChannelMessage += HandleChannelMessageOrAction;
            ConnectionManager.ChannelAction += HandleChannelMessageOrAction;

            RebuildRegexCache();
        }

        public virtual void ReloadConfiguration(JObject newConfig)
        {
            Config = new DebounceConfig(newConfig);
            PostConfigReload();
        }

        protected virtual void PostConfigReload()
        {
            RebuildRegexCache();
        }

        protected virtual void RebuildRegexCache()
        {
            var newPatterns = new HashSet<string>();
            foreach (BounceCriterion criterion in Config.Criteria)
            {
                if (criterion.ChannelRegexes != null)
                {
                    newPatterns.UnionWith(criterion.ChannelRegexes);
                }
                if (criterion.NicknameRegexes != null)
                {
                    newPatterns.UnionWith(criterion.NicknameRegexes);
                }
            }
            RegexCache.ReplaceAllWith(newPatterns);
        }

        protected virtual void HandleUserJoin(object sender, IUserJoinedChannelEventArgs e)
        {
            BounceCriterion criterion = InterestingCriterion(e.Nickname, e.Channel);
            if (criterion == null)
            {
                return;
            }

            // are hours set?
            if (criterion.StartingHour.HasValue && criterion.EndingHour.HasValue)
            {
                HandleHourCriterion(e.Channel, e.Nickname, criterion);
            }
            else
            {
                HandleStandardCriterion(e.Channel, e.Nickname, criterion);
            }
        }

        protected virtual void HandleHourCriterion(string channel, string nickname, BounceCriterion criterion)
        {
            // find the starting hour relative to today
            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset today = new DateTimeOffset(now.Date, now.Offset);
            DateTimeOffset todayStart = today.AddHours(criterion.StartingHour.Value);
            DateTimeOffset todayEnd = today.AddHours(criterion.EndingHour.Value);

            if (todayEnd == todayStart)
            {
                // no time range
                return;
            }

            DateTimeOffset cutoff = (now >= todayStart)
                ? todayStart
                : todayStart.AddDays(-1);
            DateTimeOffset currentEnd;
            bool isActive;

            if (todayEnd > todayStart)
            {
                // daytime hours
                isActive = (now >= todayStart && now < todayEnd);
                currentEnd = todayEnd;
            }
            else
            {
                Debug.Assert(todayEnd < todayStart);

                // overnight hours
                if (now < todayEnd)
                {
                    isActive = true;
                    currentEnd = todayEnd;
                }
                else if (now >= todayStart)
                {
                    isActive = true;
                    currentEnd = todayEnd.AddDays(1);
                }
                else
                {
                    isActive = false;
                }
            }

            if (!isActive)
            {
                // the entries are not interesting anymore
                RelevantJoins.Clear();
                return;
            }

            // add this join
            RelevantJoins.Add(new JoinQuitEvent(channel, nickname, now));

            // check if the criterion is fulfilled
            if (CheckAndDebounce(channel, nickname, criterion, now))
            {
                // unban at the end time
                ConnectionManager.Timers.Register(currentEnd, () => Unban(channel, nickname));
            }
        }

        protected virtual void HandleStandardCriterion(string channel, string nickname, BounceCriterion criterion)
        {
            // add this join
            DateTimeOffset now = DateTimeOffset.Now;
            RelevantJoins.Add(new JoinQuitEvent(channel, nickname, now));

            // calculate unban time
            DateTimeOffset unbanTimestamp = now.AddMinutes(criterion.BanDurationMinutes.Value);

            // check if the criterion is fulfilled
            if (CheckAndDebounce(channel, nickname, criterion, now))
            {
                // unban after the set duration
                ConnectionManager.Timers.Register(unbanTimestamp, () => Unban(channel, nickname));
            }
        }

        protected virtual bool CheckAndDebounce(string channel, string nickname, BounceCriterion criterion, DateTimeOffset now)
        {
            // clear out all entries older than the time slice
            DateTimeOffset sliceStart = now.AddMinutes(-criterion.TimeSliceMinutes);
            RelevantJoins.RemoveAll(j => j.Timestamp < sliceStart);

            // how many are left?
            int count = RelevantJoins.Count(j => j.Nickname == nickname && j.Channel == channel);
            if (count < criterion.MaxJoinsQuitsInTimeSlice)
            {
                return false;
            }

            // kickban!
            string message = (criterion.KickMessages.Count > 0)
                ? criterion.KickMessages[RNG.Next(criterion.KickMessages.Count)]
                : null;

            ConnectionManager.ChangeChannelMode(channel, $"+b {nickname}!*@*");
            ConnectionManager.KickChannelUser(channel, nickname, message);
            return true;
        }

        protected virtual void Unban(string channel, string nickname)
        {
            ConnectionManager.ChangeChannelMode(channel, $"-b {nickname}!*@*");
        }

        protected virtual void HandleChannelMessageOrAction(object sender, IChannelMessageEventArgs e, MessageFlags flags)
        {
            BounceCriterion crit = InterestingCriterion(e.SenderNickname, channel: null);
            if (crit != null && crit.ForgetOnChannelMessage)
            {
                RelevantJoins.RemoveAll(j => j.Nickname == e.SenderNickname);
            }
        }

        protected virtual BounceCriterion InterestingCriterion(string nick, string channel = null)
        {
            foreach (BounceCriterion criterion in Config.Criteria)
            {
                if (channel != null)
                {
                    if (criterion.ChannelRegexes != null)
                    {
                        if (!criterion.ChannelRegexes.Any(r => RegexCache[r].IsMatch(channel)))
                        {
                            continue;
                        }
                    }
                }

                if (criterion.NicknameRegexes != null)
                {
                    if (!criterion.NicknameRegexes.Any(r => RegexCache[r].IsMatch(nick)))
                    {
                        continue;
                    }
                }

                return criterion;
            }

            return null;
        }
    }
}
