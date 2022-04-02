namespace Dragonfly.Umbraco9DeployTools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dragonfly.NetModels;

    public static class DragonflyExtensions
    {
        public static bool HasAnyExceptions(this StatusMessage Msg)
        {
            if (Msg.RelatedException != null)
            {
                return true;
            }

            if (Msg.InnerStatuses.Any())
            {
                return Msg.InnerStatuses.Select(n => n.RelatedException).Any(n => n != null);
            }

            return false;
        }

        public static bool HasAnyFailures(this StatusMessage Msg)
        {
            if (!Msg.Success)
            {
                return true;
            }

            if (Msg.InnerStatuses.Any())
            {
                return Msg.InnerStatuses.Any(n => n.Success != true);
            }

            return false;
        }

    }
}
