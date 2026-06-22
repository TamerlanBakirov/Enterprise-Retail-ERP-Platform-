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

        var warehouseMain = Warehouse.Create("WH-MAIN", "Central Distribution", WarehouseType.Central, nameKa: "ცენტრალური საწყობი");
        db.Warehouses.Add(warehouseMain);

        // Categories
        var beverages = Category.Create("BEV", "Beverages", nameKa: "სასმელები");
        var food = Category.Create("FOOD", "Food", nameKa: "საკვები");
        var household = Category.Create("HOME", "Household", nameKa: "საყოფაცხოვრებო");
        var dairy = Category.Create("DAIRY", "Dairy Products", nameKa: "რძის პროდუქტები");
        var bakery = Category.Create("BAKERY", "Bakery", nameKa: "საცხობი");
        var wine = Category.Create("WINE", "Wine & Spirits", nameKa: "ღვინო და სპირტიანი");
        var snacks = Category.Create("SNACK", "Snacks & Confectionery", nameKa: "საჭმელები და საკონდიტრო");
        var hygiene = Category.Create("HYG", "Personal Care", nameKa: "პირადი მოვლა");
        db.Categories.AddRange(beverages, food, household, dairy, bakery, wine, snacks, hygiene);

        // Products: (sku, name, nameKa, category, unit, retailPrice, costPrice, barcode, minStock)
        var catalog = new (string Sku, string Name, string NameKa, Category Cat, string Unit, decimal Retail, decimal Cost, string Barcode, int MinStock)[]
        {
            // Beverages
            ("BEV-001", "Borjomi Water 0.5L", "ბორჯომი 0.5ლ", beverages, "pcs", 2.50m, 1.40m, "4860001230015", 50),
            ("BEV-002", "Natakhtari Lemonade 0.5L", "ნატახტარი ლიმონათი 0.5ლ", beverages, "pcs", 2.20m, 1.20m, "4860001230022", 40),
            ("BEV-003", "Coca-Cola 1L", "კოკა-კოლა 1ლ", beverages, "pcs", 3.80m, 2.30m, "5449000000996", 30),
            ("BEV-004", "Nabeghlavi Water 1L", "ნაბეღლავი 1ლ", beverages, "pcs", 1.80m, 0.90m, "4860001230039", 60),
            ("BEV-005", "Zedazeni Lemonade Tarkhuna 0.5L", "ზედაზენი ტარხუნა 0.5ლ", beverages, "pcs", 2.40m, 1.30m, "4860001230046", 35),
            ("BEV-006", "Lagidze Waters Cream Soda", "ლაგიძის წყალი ნაღების", beverages, "pcs", 3.20m, 1.80m, "4860001230053", 25),

            // Food
            ("FOOD-001", "Khachapuri Imeruli", "ხაჭაპური იმერული", food, "pcs", 6.50m, 3.50m, "4860002340011", 20),
            ("FOOD-002", "Lobiani", "ლობიანი", food, "pcs", 4.50m, 2.50m, "4860002340042", 15),
            ("FOOD-003", "Churchkhela Walnut", "ჩურჩხელა ნიგვზის", snacks, "pcs", 5.00m, 2.80m, "4860002340059", 30),
            ("FOOD-004", "Tkemali Sauce 350ml", "ტყემალი 350მლ", food, "pcs", 4.80m, 2.60m, "4860002340066", 25),
            ("FOOD-005", "Adjika Hot 200g", "აჯიკა ცხარე 200გ", food, "pcs", 3.90m, 2.10m, "4860002340073", 20),
            ("FOOD-006", "Tonis Puri", "თონის პური", bakery, "pcs", 1.50m, 0.70m, "4860002340080", 40),

            // Dairy
            ("DAIRY-001", "Sulguni Cheese 1kg", "სულგუნი 1კგ", dairy, "kg", 14.90m, 9.00m, "4860002340028", 10),
            ("DAIRY-002", "Imeruli Cheese 1kg", "იმერული ყველი 1კგ", dairy, "kg", 12.50m, 7.50m, "4860002340097", 10),
            ("DAIRY-003", "Matsoni 500ml", "მაწონი 500მლ", dairy, "pcs", 3.20m, 1.80m, "4860002340104", 30),
            ("DAIRY-004", "Nadughi 200g", "ნადუღი 200გ", dairy, "pcs", 2.80m, 1.50m, "4860002340111", 20),

            // Bakery
            ("BAKERY-001", "Bread White", "თეთრი პური", bakery, "pcs", 1.20m, 0.60m, "4860002340035", 50),
            ("BAKERY-002", "Lavash", "ლავაში", bakery, "pcs", 1.00m, 0.50m, "4860002340128", 40),
            ("BAKERY-003", "Shotis Puri", "შოთის პური", bakery, "pcs", 1.80m, 0.90m, "4860002340135", 35),

            // Wine
            ("WINE-001", "Saperavi Red Dry 0.75L", "საფერავი წითელი მშრალი 0.75ლ", wine, "pcs", 18.00m, 10.00m, "4860005670011", 15),
            ("WINE-002", "Tsinandali White Dry 0.75L", "წინანდალი თეთრი მშრალი 0.75ლ", wine, "pcs", 16.50m, 9.50m, "4860005670028", 15),
            ("WINE-003", "Kindzmarauli Semi-Sweet 0.75L", "კინძმარაული ნახევრადტკბილი 0.75ლ", wine, "pcs", 22.00m, 13.00m, "4860005670035", 10),
            ("WINE-004", "Chacha Grape Spirit 0.5L", "ჭაჭა 0.5ლ", wine, "pcs", 15.00m, 8.00m, "4860005670042", 10),

            // Household
            ("HOME-001", "Dish Soap 500ml", "ჭურჭლის სარეცხი 500მლ", household, "pcs", 4.30m, 2.50m, "4860003450017", 20),
            ("HOME-002", "Paper Towels 2pk", "ქაღალდის ხელსახოცი 2ც", household, "pcs", 5.60m, 3.40m, "4860003450024", 25),
            ("HOME-003", "Laundry Detergent 1kg", "სარეცხი ფხვნილი 1კგ", household, "pcs", 8.90m, 5.20m, "4860003450031", 15),

            // Snacks
            ("SNACK-001", "Tklapi Fruit Leather", "ტყლაპი", snacks, "pcs", 3.50m, 1.80m, "4860006780015", 30),
            ("SNACK-002", "Gozinaki Honey Walnut Bar", "გოზინაყი", snacks, "pcs", 4.50m, 2.50m, "4860006780022", 25),

            // Personal Care
            ("HYG-001", "Toothpaste 75ml", "კბილის პასტა 75მლ", hygiene, "pcs", 5.50m, 3.00m, "4860007890018", 20),
            ("HYG-002", "Shampoo 400ml", "შამპუნი 400მლ", hygiene, "pcs", 7.80m, 4.50m, "4860007890025", 15),
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
                nameKa: item.NameKa,
                minStockLevel: item.MinStock,
                reorderPoint: item.MinStock * 2);
            db.Products.Add(product);

            db.ProductBarcodes.Add(ProductBarcode.Create(product.Id, item.Barcode, BarcodeType.Ean13, isPrimary: true));

            // Stock in store warehouse
            var stock = StockLevel.Create(product.Id, warehouse.Id, costPrice: item.Cost);
            stock.AddStock(100m);
            db.StockLevels.Add(stock);

            // Stock in central warehouse (higher quantities)
            var centralStock = StockLevel.Create(product.Id, warehouseMain.Id, costPrice: item.Cost);
            centralStock.AddStock(500m);
            db.StockLevels.Add(centralStock);

            db.PriceListItems.Add(PriceListItem.Create(priceList.Id, product.Id, item.Retail));
        }

        // Customers - Georgian names with loyalty programs
        db.Customers.Add(Customer.Create("CUST-0001", "Giorgi", "Beridze", "გიორგი", "ბერიძე"));
        db.Customers.Add(Customer.Create("CUST-0002", "Nino", "Kapanadze", "ნინო", "კაპანაძე"));
        db.Customers.Add(Customer.Create("CUST-0003", "Levan", "Tsiklauri", "ლევან", "წიკლაური"));
        db.Customers.Add(Customer.Create("CUST-0004", "Tamar", "Gelashvili", "თამარ", "გელაშვილი"));
        db.Customers.Add(Customer.Create("CUST-0005", "Dato", "Kvaratskhelia", "დათო", "კვარაცხელია"));
        db.Customers.Add(Customer.Create("CUST-0006", "Maia", "Jokhadze", "მაია", "ჯოხაძე"));
        db.Customers.Add(Customer.Create("CUST-0007", "Zurab", "Shalikashvili", "ზურაბ", "შალიკაშვილი"));
        db.Customers.Add(Customer.Create("CUST-0008", "Ketevan", "Davitashvili", "ქეთევან", "დავითაშვილი"));

        // Suppliers - Georgian distribution companies
        db.Suppliers.Add(Supplier.Create("SUP-001", "Tbilisi Distribution Ltd", nameKa: "თბილისის დისტრიბუცია", tin: "405200111"));
        db.Suppliers.Add(Supplier.Create("SUP-002", "Kakheti Food Supply", nameKa: "კახეთის საკვები", tin: "405200222"));
        db.Suppliers.Add(Supplier.Create("SUP-003", "Georgian Beverages Co", nameKa: "ქართული სასმელები", tin: "405200333"));
        db.Suppliers.Add(Supplier.Create("SUP-004", "Caucasus Dairy Products", nameKa: "კავკასიის რძის პროდუქტები", tin: "405200444"));
        db.Suppliers.Add(Supplier.Create("SUP-005", "Telavi Wine Cellar", nameKa: "თელავის ღვინის სარდაფი", tin: "405200555"));
        db.Suppliers.Add(Supplier.Create("SUP-006", "Batumi Import Export", nameKa: "ბათუმის იმპორტ-ექსპორტი", tin: "405200666"));

        // POS terminal + open session ready for sales
        var terminal = PosTerminal.Create("POS-01", store.Id, "Register 1", TerminalType.Register);
        db.PosTerminals.Add(terminal);

        var session = PosSession.Create(terminal.Id, admin.Id, openingBalance: 200m);
        db.PosSessions.Add(session);

        await db.SaveChangesAsync();
        logger.LogInformation("Demo data seeded: {Products} products, {Customers} customers, {Suppliers} suppliers, store {Store}, open POS session {Session}",
            catalog.Length, 8, 6, store.Code, session.Id);
    }
}
