using System;

namespace SharpIrcBot
{
    public delegate void SharpIrcBotEventHandler<TEventArgs>(object sender, TEventArgs e, MessageFlags flags);
}
