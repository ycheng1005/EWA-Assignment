using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// TODO: Additional using statements (Stripe)
using Stripe;
using Stripe.Checkout;

namespace Demo.Controllers;

public class ProductController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;
    private readonly IConfiguration cf;

    // TODO: Inject IConfiguration
    public ProductController(DB db, IWebHostEnvironment en, Helper hp, IConfiguration cf)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
        this.cf = cf;

        // TODO: Stripe configuration (API key)
        StripeConfiguration.ApiKey = cf["Stripe:SK"];
    }

    // POST: Product/UpdateCart
    [HttpPost]
    public IActionResult UpdateCart(string productId, int quantity)
    {
        var cart = hp.GetCart();

        if (quantity >= 1 && quantity <= 10)
        {
            cart[productId] = quantity;
        }
        else
        {
            cart.Remove(productId);
        }

        hp.SetCart(cart);

        return Redirect(Request.Headers.Referer.ToString());
    }

    // GET: Product/Index
    public IActionResult Index()
    {
        ViewBag.Cart = hp.GetCart();
        var model = db.Products;
        
        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    // GET: Product/Detail
    public IActionResult Detail(string? id)
    {
        var model = db.Products.Find(id);

        if (model == null) return RedirectToAction("Index");

        return View(model);
    }

    // GET: Product/ShoppingCart
    public IActionResult ShoppingCart()
    {
        var cart = hp.GetCart();
        
        var model = cart.Select(item => new CartItem
        {
            Product = db.Products.Find(item.Key)!,
            Quantity = item.Value
        });

        if (Request.IsAjax()) return PartialView("_ShoppingCart", model);

        return View(model);
    }


    // GET: Product/Order
    [Authorize(Roles = "Member")]
    public IActionResult Order()
    {
        var model = db.Orders
                      .Include(o => o.OrderLines)
                      .Where(o => o.MemberEmail == User.Identity!.Name)
                      .OrderByDescending(o => o.Id);

        return View(model);
    }

    // GET: Product/OrderDetail
    [Authorize(Roles = "Member")]
    public IActionResult OrderDetail(int id)
    {
        var model = db.Orders
                      .Include(o => o.OrderLines)
                      .ThenInclude(ol => ol.Product)
                      .FirstOrDefault(o => o.Id == id && o.MemberEmail == User.Identity!.Name);

        if (model == null) return RedirectToAction("Order");

        return View(model);
    }

    // POST: Product/ResetAll
    [HttpPost]
    public ActionResult ResetAll()
    {
        // NOTE: "TRUNCATE TABLE [Orders]" only work if without FK constraint

        // Empty [Orders] and [OrderLines] tables
        db.Orders.RemoveRange(db.Orders);
        db.SaveChanges();

        // Reseed [Orders] and [OrderLines] tables
        db.Database.ExecuteSqlRaw(@"
            DBCC CHECKIDENT (Orders, RESEED, 0);
            DBCC CHECKIDENT (OrderLines, RESEED, 0);
        ");

        return RedirectToAction("Order");
    }

    // NEW --------------------------------------------------------------------

    // POST: Product/Checkout
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Checkout()
    {
        var cart = hp.GetCart();
        if (cart.Count == 0) return RedirectToAction("ShoppingCart");

        // TODO: Create a Stripe checkout session
        // - domain    : string
        // - metadata  : Dictionary<string, string>
        // - lineItems : List<SessionLineItemOptions>
        // - options   : SessionCreateOptions
        var domain = Url.Action("", "", null, "https");

        var metadata = cart.ToDictionary(x => x.Key, x => x.Value.ToString());

        var lineItems = new List<SessionLineItemOptions>();

        foreach (var (productId, quantity) in cart)
        {
            var product = db.Products.Find(productId)!;

            lineItems.Add(new()
            {
                PriceData = new()
                {
                    ProductData = new()
                    {
                        Name = $"{product.Id} - {product.Name}",
                    },
                    Currency = "myr",
                    UnitAmount = Convert.ToInt64(product.Price * 100),
                },
                Quantity = quantity,
            });
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = domain + "Product/Success?sessionId={CHECKOUT_SESSION_ID}",
            CancelUrl = domain + "Product/Cancel",
            CustomerEmail = User.Identity!.Name,
            ClientReferenceId = "XXX", // Customize: Shopping Cart Id, Order Id, etc,
            Metadata = metadata, // Customize: Shopping Cart
            LineItems = lineItems,
        };

        var session = new SessionService().Create(options);

        // TODO: Redirect to Stripe checkout page
        return Redirect(session.Url);
    }

    public IActionResult Cancel()
    {
        return View();
    }

    public IActionResult Success(string? sessionId)
    {
        // TODO: If session id available [AND] request from Stripe
        // - Clear shopping cart
        // - Fulfill order --> order id
        // - Redirect to same page (without session id)
        if (sessionId != null && Request.Headers.Referer == "https://checkout.stripe.com/")
        {
            hp.SetCart(null);

            // NOTE: Comment the following 2 lines if using Stripe CLI + webhook
            var session = new SessionService().Get(sessionId);
            TempData["OrderId"] = FulfillOrder(session);

            return RedirectToAction("Success");
        }

        return View();
    }

    private int FulfillOrder(Session session /* TODO */)
    {
        // TODO: Insert order and orderlines based on session data
        
        // 1. Create [Order] (parent record)
        var order = new Order
        {
            Date = DateTime.Today,
            MemberEmail = session.CustomerEmail // TODO
        };
        db.Orders.Add(order);

        // 2. Create [OrderLine] (child record)
        foreach (var (productId, quantity) in session.Metadata) // TODO
        {
            order.OrderLines.Add(new()
            {
                Price = db.Products.Find(productId)!.Price,
                Quantity = int.Parse(quantity),
                ProductId = productId
            });
        }

        // 3. Save changes
        db.SaveChanges();

        return order.Id;
    }

    // TODO: Webhook
    [Route("Webhook")]
    public async Task<IActionResult> Webhook()
    {
        var secret = cf["Stripe:WH"];

        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        var stripeEvent = EventUtility.ConstructEvent(
            json,
            Request.Headers["Stripe-Signature"],
            secret
        );

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            Console.WriteLine("----- Checkout Session Completed -----");
            var session = stripeEvent.Data.Object as Session;
            FulfillOrder(session!);
        }

        return Ok();
    }

}
