using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using SharpIrcBot;

namespace LinkInfo
{
    public class TissCourseResolverPlugin : ILinkResolverPlugin
    {
        private static readonly ILogger Logger = SharpIrcBotUtil.LoggerFactory.CreateLogger<TissCourseResolverPlugin>();

        public const string TissHostname = "tiss.tuwien.ac.at";
        public const string EducationDetailsPath = "/course/educationDetails.xhtml";
        public const string CourseDetailsPath = "/course/courseDetails.xhtml";
        public const string SemesterVariable = "semester";
        public const string CourseNumberVariable = "courseNr";
        public const string TissCoursesApiPattern = "https://tiss.tuwien.ac.at/api/course/{0}-{1}";
        public static readonly XNamespace CourseNamespace = "https://tiss.tuwien.ac.at/api/schemas/course/v10";
        public static readonly XNamespace LangNamespace = "https://tiss.tuwien.ac.at/api/schemas/i18n/v10";

        public LinkInfoConfig LinkInfoConfig { get; set; }

        public TissCourseResolverPlugin(JObject config, LinkInfoConfig linkInfoConfig)
        {
            LinkInfoConfig = linkInfoConfig;
        }

        public LinkAndInfo ResolveLink(LinkToResolve link)
        {
            Uri theLink = link.OriginalLinkOrLink;

            if (theLink.Scheme != "http" && theLink.Scheme != "https")
            {
                return null;
            }

            if (theLink.Host != TissHostname)
            {
                return null;
            }

            if (theLink.AbsolutePath != EducationDetailsPath && theLink.AbsolutePath != CourseDetailsPath)
            {
                return null;
            }

            string semester, courseNr;
            StringValues values;
            IDictionary<string, StringValues> queryValues = QueryHelpers.ParseQuery(theLink.Query);

            if (!queryValues.TryGetValue(SemesterVariable, out values))
            {
                return null;
            }
            if (values.Count == 0)
            {
                return null;
            }
            semester = values;

            if (!queryValues.TryGetValue(CourseNumberVariable, out values))
            {
                return null;
            }
            if (values.Count == 0)
            {
                return null;
            }
            courseNr = values;

            Uri tissCoursesApiUri = new Uri(string.Format(
                TissCoursesApiPattern,
                Uri.EscapeDataString(courseNr),
                Uri.EscapeDataString(semester)
            ));

            XDocument doc;
            try
            {
                try
                {
                    var client = new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds(LinkInfoConfig.TimeoutSeconds)
                    };

                    using (var request = new HttpRequestMessage(HttpMethod.Get, tissCoursesApiUri))
                    {
                        request.Headers.UserAgent.TryParseAdd(LinkInfoConfig.FakeUserAgent);

                        using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).SyncWait())
                        using (Stream responseStream = response.Content.ReadAsStreamAsync().SyncWait())
                        {
                            doc = XDocument.Load(responseStream);
                        }
                    }
                }
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                {
                    // timed out
                    return link.ToResult(FetchErrorLevel.TransientError, "TISS course (detail fetching timed out)");
                }

                XElement courseElement = doc
                    .Element(CourseNamespace + "tuvienna")
                    .Element(CourseNamespace + "course");
                string realCourseNumber = courseElement
                    .Element(CourseNamespace + "courseNumber")
                    .Value;
                string courseType = courseElement
                    .Element(CourseNamespace + "courseType")
                    .Value;
                string title = courseElement
                    .Element(CourseNamespace + "title")
                    .Element(LangNamespace + "de")
                    .Value;

                string formattedCourseNumber = realCourseNumber.Substring(0, 3) + "." + realCourseNumber.Substring(3);

                return link.ToResult(FetchErrorLevel.Success, $"TISS course: {formattedCourseNumber} {courseType} {title}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("image info: {Exception}", ex);
                return link.ToResult(FetchErrorLevel.TransientError, "TISS course (exception thrown)");
            }
        }
    }
}
