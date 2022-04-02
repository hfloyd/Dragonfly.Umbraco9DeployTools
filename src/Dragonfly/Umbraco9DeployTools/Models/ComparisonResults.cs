namespace Dragonfly.Umbraco9DeployTools.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dragonfly.NetHelpers;
    using Dragonfly.NetModels;

    public class ComparisonResults
    {
        // public StatusMessage Status { get; set; }
        public Workspace LocalEnvironment { get; set; }
        public Workspace RemoteEnvironment { get; set; }
        public INodesDataFile LocalDataFileInfo { get; set; }
        public INodesDataFile RemoteDataFileInfo { get; set; }
        public IEnumerable<ContentNodeDataItem> ContentLocalNotRemote { get; set; }
        public IEnumerable<ContentNodeDataItem> ContentRemoteNotLocal { get; set; }
        public IEnumerable<NodeItemMatches> ContentMatchingNodes { get; set; }

        public ComparisonResults()
        {
            this.ContentMatchingNodes = new List<NodeItemMatches>();
            this.ContentLocalNotRemote = new List<ContentNodeDataItem>();
            this.ContentRemoteNotLocal = new List<ContentNodeDataItem>();

        }

    }

    public class NodeItemMatches
    {
        public INodeDataItem LocalNode { get; set; }
        public INodeDataItem RemoteNode { get; set; }

        public List<LocalRemoteComparison> Comparisons { get; set; }
        public int NumberOfDifferences { get; set; }

        public NodeItemMatches()
        {
            this.Comparisons = new List<LocalRemoteComparison>();
        }
        public int CountDifferences(bool ExcludeDifferentIrrelevant)
        {
            var diff1 = Comparisons.Count(n => n.Result == ComparisonResult.Different);
            var diff2 = Comparisons.Count(n => n.Result == ComparisonResult.DifferentLocalPreferred);
            var diff3 = Comparisons.Count(n => n.Result == ComparisonResult.DifferentRemotePreferred);
            var diff4 = ExcludeDifferentIrrelevant ? 0 : Comparisons.Count(n => n.Result == ComparisonResult.DifferentIrrelevant);

            var allDiffs = diff1 + diff2 + diff3 + diff4;
            return allDiffs;
        }
    }

    public class LocalRemoteComparison
    {
        public string PropertyName { get; set; }
        public object LocalValue { get; set; }
        public object RemoteValue { get; set; }
        public ComparisonResult Result { get; set; }
    }

    public enum ComparisonResult
    {
        Same,
        Different,
        DifferentRemotePreferred,
        DifferentLocalPreferred,
        DifferentIrrelevant,
        Unknown
    }

    public static class ComparisonResultExtensions
    {
        public static string GetFriendlyString(this ComparisonResult ResultEnum)
        {
            var text = ResultEnum.ToString().SplitCamelCase(" ");
            if (text.Contains(" "))
            {
                text = text.Replace("Different ", "Different (");
                text += ")";
            }

            return text;
        }
    }
}
