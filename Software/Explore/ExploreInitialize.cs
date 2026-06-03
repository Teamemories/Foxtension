namespace Foxtension.Software.Explore
{
    public enum ExploreTarget
    {
        File,
        Folder
    }
    public enum ExploreRequest
    {
        Create,
        Delete,
        Rename,
        Open,
        Hide,
        Unhide,
        SetReadonly,
        UnsetReadonly,
        Copy,
        Cut,
        Compress,
        Extract
    }
    public sealed class ExploreInitialize
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string NewName { get; set; } = string.Empty;
        public string NewPath { get; set; } = string.Empty;
        public bool Override { get; set; } = false;
        public string Username { get; set; } = string.Empty;
        public string? ItemSharedName { get; set; }
    }
}
