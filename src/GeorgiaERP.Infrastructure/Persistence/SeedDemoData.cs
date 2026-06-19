using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Pricing;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Persistence;

/// <summary>
/// Seeds a realistic demo dataset (company, store, warehouse, products, stock,
/// customers, suppliers, an open POS session) so a fresh install is immediately
/// usable for demos and manual testing. Runs only when no company exists yet.
/// Enabled by passing --seed-demo or setting Seed:Demo=true.
/// </summary>
public static class SeedDemoData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        if (await db.Companies.AnyAsync())
        {
            logger.LogInformation("Demo data skipped: company data already present");
            return;
        }

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin is null)
        {
            logger.LogWarning("Demo data skipped: admin user not found (run base seed first)");
            return;
        }

        // Organization
        var company = Company.Create("DEMO", "Demo Retail Georgia LLC", "405123456", isVatPayer: true, nameKa: "დემო რითეილ ჯორჯია");
        db.Companies.Add(company);

        var store = Store.Create("ST-TBS", "Tbilisi Central", StoreType.Retail, nameKa: "თბილისი ცენტრალური");
        db.Stores.Add(store);

        var warehouse = Warehouse.Create("WH-TBS", "Tbilisi Store Stock", WarehouseType.Store, nameKa: "თბილისის საწყობი");
        warehouse.LinkToStore(store.Id);
        db.Warehouses.Add(warehouse);

        // Categories
        var beverages = Category.Create("BEV", "Beverages", nameKa: "სასმელები");
        var food = Category.Create("FOOD", "Food", nameKa: "საკვები");
        var household = Category.Create("HOME", "Household", nameKa: "საყოფაცხოვრებო");
        db.Categories.AddRange(beverages, food, household);

        // Products: (sku, name, nameKa, category, unit, retailPrice, costPrice, barcode)
        var catalog = new (string Sku, string Name, string NameKa, Category Cat, string Unit, decimal Retail, decimal Cost, string Barcode)[]
        {
            ("BEV-001", "Borjomi Water 0.5L", "ბორჯომი 0.5ლ", beverages, "pcs", 2.50m, 1.40m, "4860001230015"),
            ("BEV-002", "Natakhtari Lemonade 0.5L", "ნატახტარი ლიმონათი", beverages, "pcs", 2.20m, 1.20m, "4860001230022"),
            ("BEV-003", "Coca-Cola 1L", "კოკა-კოლა 1ლ", beverages, "pcs", 3.80m, 2.30m, "5449000000996"),
            ("FOOD-001", "Khachapuri Imeruli", "ხაჭაპური იმერული", food, "pcs", 6.50m, 3.50m, "4860002340011"),
            ("FOOD-002", "Sulguni Cheese 1kg", "სულგუნი 1კგ", food, "kg", 14.90m, 9.00m, "4860002340028"),
            ("FOOD-003", "Bread White", "თეთრი პური", food, "pcs", 1.20m, 0.60m, "4860002340035"),
            ("HOME-001", "Dish Soap 500ml", "ჭურჭლის სარეცხი", household, "pcs", 4.30m, 2.50m, "4860003450017"),
            ("HOME-002", "Paper Towels 2pk", "ქაღალდის ხელსახოცი", household, "pcs", 5.60m, 3.40m, "4860003450024"),
        };

        var priceList = PriceList.Create("RETAIL", "Retail Prices", PriceType.Retail, DateTimeOffset.UtcNow, nameKa: "საცალო ფასები");
        db.PriceLists.Add(priceList);

        foreach (var item in catalog)
        {
            var product = Product.Create(
                sku: item.Sku,
                name: item.Name,
                categoryId: item.Cat.Id,
                unitOfMeasure: item.Unit,
                vatApplicable: true,
                nameKa: item.NameKa);
            db.Products.Add(product);

            db.ProductBarcodes.Add(ProductBarcode.Create(product.Id, item.Barcode, BarcodeType.Ean13, isPrimary: true));

            var stock = StockLevel.Create(product.Id, warehouse.Id, costPrice: item.Cost);
            stock.AddStock(100m);
            db.StockLevels.Add(stock);

            db.PriceListItems.Add(PriceListItem.Create(priceList.Id, product.Id, item.Retail));
        }

        // Customers
        db.Customers.Add(Customer.Create("CUST-0001", "Giorgi", "Beridze", "გიორგი", "ბერიძე"));
        db.Customers.Add(Customer.Create("CUST-0002", "Nino", "Kapanadze", "ნინო", "კაპანაძე"));
        db.Customers.Add(Customer.Create("CUST-0003", "Levan", "Tsiklauri", "ლევან", "წიკლაური"));

        // Suppliers
        db.Suppliers.Add(Supplier.Create("SUP-001", "Tbilisi Distribution Ltd", nameKa: "თბილისის დისტრიბუცია", tin: "405200111"));
        db.Suppliers.Add(Supplier.Create("SUP-002", "Kakheti Food Supply", nameKa: "კახეთის საკვები", tin: "405200222"));

        // POS terminal + open session ready for sales
        var terminal = PosTerminal.Create("POS-01", store.Id, "Register 1", TerminalType.Register);
        db.PosTerminals.Add(terminal);

        var session = PosSession.Create(terminal.Id, admin.Id, openingBalance: 200m);
        db.PosSessions.Add(session);

        await db.SaveChangesAsync();
        logger.LogInformation("Demo data seeded: {Products} products, store {Store}, open POS session {Session}",
            catalog.Length, store.Code, session.Id);
    }
}
