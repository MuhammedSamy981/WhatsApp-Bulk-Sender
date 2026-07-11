using WhatsAppBulkSender.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Register HttpClient for UltraMsg API calls
builder.Services.AddHttpClient("UltraMsgClient", client =>
{
    client.BaseAddress = new Uri("https://api.ultramsg.com/");
});
// Register WhatsApp service as singleton (one session per app)
builder.Services.AddSingleton<WhatsAppService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// SignalR Hub
app.MapHub<WhatsAppHub>("/whatsappHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
