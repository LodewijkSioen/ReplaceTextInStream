namespace ReplaceTextInStream.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            var newStream = new MemoryStream();
            var originalStream = context.Response.Body;
            context.Response.Body = newStream;

            await next(context);

            IStreamingReplacer replacer = context.Request.Path switch
            {
                var p when p.StartsWithSegments("/pipes") => new UsingPipes(),
                var p when p.StartsWithSegments("/regex") => new UsingRegexReplace(),
                var p when p.StartsWithSegments("/stream") => new UsingStreamReader(),
                var p when p.StartsWithSegments("/string") => new UsingStringReplace(),
                _ => new UsingPipes()
            };
            newStream.Position = 0;
            await replacer.Replace(newStream, originalStream, "lorem", "schorem", context.RequestAborted);
            context.Response.Body = originalStream;
        });

        var text = File.ReadAllText(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "LoremIpsum.txt"));

        app.MapGet("/", () => Results.Redirect("/pipes"));
        app.MapGet("/pipes", () => Results.Ok(text));
        app.MapGet("/regex", () => Results.Ok(text));
        app.MapGet("/stream", () => Results.Ok(text));
        app.MapGet("/string", () => Results.Ok(text));

        app.Run();
    }
}