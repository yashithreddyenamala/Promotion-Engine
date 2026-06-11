namespace PromotionEngine
{
    // --- Core Models ---
    public class Product
    {
        public char Id { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CartItem
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }

    // --- Interface for the Strategy of Promotion ---
    public interface IPromotion
    {
        void ApplyPromotion(Dictionary<char, int> itemQuantities, Dictionary<char, Product> products, ref decimal totalValue);
    }

    // --- Types of Promotion ---
    public class MultiBuyPromotion : IPromotion
    {
        private readonly char _sku;
        private readonly int _requiredQuantity;
        private readonly decimal _promoPrice;

        public MultiBuyPromotion(char sku, int requiredQuantity, decimal promoPrice)
        {
            _sku = sku;
            _requiredQuantity = requiredQuantity;
            _promoPrice = promoPrice;
        }

        public void ApplyPromotion(Dictionary<char, int> itemQuantities, Dictionary<char, Product> products, ref decimal totalValue)
        {
            if (itemQuantities.TryGetValue(_sku, out int quantity) && quantity >= _requiredQuantity)
            {
                int promoBatches = quantity / _requiredQuantity;
                int remainingItems = quantity % _requiredQuantity;

                totalValue += promoBatches * _promoPrice;
                itemQuantities[_sku] = remainingItems; 
            }
        }
    }

    public class ComboPromotion : IPromotion
    {
        private readonly char _sku1;
        private readonly char _sku2;
        private readonly decimal _promoPrice;

        public ComboPromotion(char sku1, char sku2, decimal promoPrice)
        {
            _sku1 = sku1;
            _sku2 = sku2;
            _promoPrice = promoPrice;
        }

        public void ApplyPromotion(Dictionary<char, int> itemQuantities, Dictionary<char, Product> products, ref decimal totalValue)
        {
            if (itemQuantities.TryGetValue(_sku1, out int q1) && itemQuantities.TryGetValue(_sku2, out int q2))
            {
                int comboPairs = Math.Min(q1, q2);

                totalValue += comboPairs * _promoPrice;
                itemQuantities[_sku1] -= comboPairs;
                itemQuantities[_sku2] -= comboPairs;
            }
        }
    }

    // --- Calculator ---
    public class PromotionEngineCalculator
    {
        private readonly Dictionary<char, Product> _productCatalog;
        private readonly List<IPromotion> _activePromotions;

        public PromotionEngineCalculator(IEnumerable<Product> catalog, IEnumerable<IPromotion> promotions)
        {
            _productCatalog = catalog.ToDictionary(p => p.Id);
            _activePromotions = promotions.ToList();
        }

        public decimal CalculateTotal(IEnumerable<CartItem> cart)
        {
            decimal totalValue = 0;
            var remainingQuantities = cart.ToDictionary(item => item.Product.Id, item => item.Quantity);

            foreach (var promotion in _activePromotions)
            {
                promotion.ApplyPromotion(remainingQuantities, _productCatalog, ref totalValue);
            }

            foreach (var kvp in remainingQuantities)
            {
                char sku = kvp.Key;
                int remainingQty = kvp.Value;

                if (remainingQty > 0 && _productCatalog.TryGetValue(sku, out var product))
                {
                    totalValue += remainingQty * product.UnitPrice;
                }
            }

            return totalValue;
        }
    }

    // --- Interactive Step Entry Point for People to Enter their own Quantity for Items ---
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1. Configuration Establishment (From Document)
            var prodA = new Product { Id = 'A', UnitPrice = 50 };
            var prodB = new Product { Id = 'B', UnitPrice = 30 };
            var prodC = new Product { Id = 'C', UnitPrice = 20 };
            var prodD = new Product { Id = 'D', UnitPrice = 15 };

            var catalog = new List<Product> { prodA, prodB, prodC, prodD };

            var promotions = new List<IPromotion>
            {
                new MultiBuyPromotion('A', 3, 130),
                new MultiBuyPromotion('B', 2, 45), 
                new ComboPromotion('C', 'D', 30)   
            };

            var engine = new PromotionEngineCalculator(catalog, promotions);

            // 2. Displaying Promotion and Asking for the Quantity User wants as Input
            Console.WriteLine("=== PROMOTION ENGINE TERMINAL ===");
            Console.WriteLine("Active Promotions: 3xA=130, 2xB=45, C+D=30\n");

            var cart = new List<CartItem>();

            foreach (var product in catalog)
            {
                Console.Write($"Enter quantity for Product {product.Id} (Price: {product.UnitPrice}): ");
                string input = Console.ReadLine();
                
                // Validate that user entered a proper number, default to 0 if blank/invalid
                if (!int.TryParse(input, out int quantity) || quantity < 0)
                {
                    quantity = 0;
                }

                if (quantity > 0)
                {
                    cart.Add(new CartItem { Product = product, Quantity = quantity });
                }
            }

            // 3. Final Price after Computed Processing
            Console.WriteLine("\n--------------------------------");
            decimal finalPrice = engine.CalculateTotal(cart);
            Console.WriteLine($"Final Order Total (Using Promotions): {finalPrice}");
            Console.WriteLine("--------------------------------");
        }
    }
}
