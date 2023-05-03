namespace Energy.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class InfoAttribute : Attribute
    {
        public string Title { get; }
        public string Author { get; }
        public string Version { get; }

        public InfoAttribute(string title,string author, string version)
        {
            Title = title;
            Version = version;
            Author = author;
        }
        public InfoAttribute(string title, string author)
        {
            Title = title;
            Version = GetType().Assembly.GetName().Version.ToString();
            Author = author;
        }
    }
}
