using Microsoft.Extensions.Logging;

namespace Pictora
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("OpenSans-Bold.ttf", "OpenSans-Bold");
                    fonts.AddFont("OpenSans-BoldItalic.ttf", "OpenSans-BoldItalic");
                    fonts.AddFont("OpenSans-ExtraBold.ttf", "OpenSans-ExtraBold");
                    fonts.AddFont("OpenSans-Italic.ttf", "OpenSans-Italic");
                    fonts.AddFont("OpenSans-LightItalic.ttf", "OpenSans-LightItalic");
                    fonts.AddFont("Nunito-Bold.ttf", "Nunito-Bold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
