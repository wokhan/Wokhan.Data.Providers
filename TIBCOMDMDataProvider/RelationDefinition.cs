namespace Wokhan.Data.Providers.Bases
{
    public class RelationDefinition
    {
        public string Name;
        public string Source;
        public string SourceAttribute;
        public string Target;
        public string TargetAttribute;
        public bool IsSoftlink;
        public bool IsReversed;
        public bool IsCartesian;
        public bool IsHidden;

        public bool RetrieveAll;

        public string TechnicalName;
        public string TechnicalSourceAttr;
        public string TechnicalTargetAttr;
        public string TechnicalSource;
        public string TechnicalTarget;
    }
}