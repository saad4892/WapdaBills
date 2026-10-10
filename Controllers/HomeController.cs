using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WapdaBill.Models;

namespace WapdaBill.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View(new LookupForm());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
