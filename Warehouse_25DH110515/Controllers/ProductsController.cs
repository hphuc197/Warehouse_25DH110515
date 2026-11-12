using System;
using System.Linq;
using System.Web.Mvc;
using Warehouse_25DH110515.Models;

namespace Warehouse_25DH110515.Controllers
{
    public class ProductsController : Controller
    {
        private Warehouse_25DH110515Entities db = new Warehouse_25DH110515Entities();

        // CÂU 1 & CÂU 2: Hiển thị danh sách kèm trạng thái tồn kho và nút xóa sản phẩm hết hàng
        public ActionResult Index()
        {
            var products = db.Products.ToList();
            return View(products);
        }

        // CÂU 2b: Xóa sản phẩm hết hàng
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var product = db.Products.Find(id);
            if (product == null)
            {
                TempData["Error"] = "Mã sản phẩm không tồn tại!";
                return RedirectToAction("Index");
            }

            if (product.Quantity == 0)
            {
                db.Products.Remove(product);
                db.SaveChanges();
                TempData["Message"] = "Xóa sản phẩm hết hàng thành công!";
            }
            else
            {
                TempData["Error"] = "Chỉ được phép xóa sản phẩm có số lượng bằng 0 (hết hàng)!";
            }
            return RedirectToAction("Index");
        }

        // CÂU 3: Lọc và sắp xếp theo tồn kho
        public ActionResult Filter(string category, int? minQty, int? maxQty, string sortOrder)
        {
            var categories = db.Products.Select(p => p.Category).Distinct().ToList();
            ViewBag.Categories = new SelectList(categories);

            var query = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (minQty.HasValue && maxQty.HasValue)
            {
                if (minQty > maxQty || minQty < 0 || maxQty < 0)
                {
                    ModelState.AddModelError("", "Khoảng số lượng không hợp lệ (Từ số lượng phải nhỏ hơn hoặc bằng Đến số lượng và không được âm).");
                }
                else
                {
                    query = query.Where(p => p.Quantity >= minQty.Value && p.Quantity <= maxQty.Value);
                }
            }

            // Sắp xếp
            ViewBag.SortParam = sortOrder == "asc" ? "desc" : "asc";
            if (sortOrder == "asc")
            {
                query = query.OrderBy(p => p.Quantity);
            }
            else if (sortOrder == "desc")
            {
                query = query.OrderByDescending(p => p.Quantity);
            }

            return View(query.ToList());
        }

        // CÂU 4a: Nhập kho (GET)
        public ActionResult Import(int? id)
        {
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName");
            return View();
        }

        // CÂU 4a: Nhập kho (POST)
        [HttpPost]
        public ActionResult Import(int productID, int quantityToAdd)
        {
            if (quantityToAdd <= 0)
            {
                ModelState.AddModelError("", "Số lượng bổ sung phải là số nguyên dương.");
                ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName", productID);
                return View();
            }

            var product = db.Products.Find(productID);
            if (product != null)
            {
                product.Quantity += quantityToAdd;
                db.SaveChanges();
                TempData["Message"] = "Nhập kho thành công!";
                return RedirectToAction("Index");
            }
            return View();
        }

        // CÂU 4b: Ghi nhận hàng hỏng (GET)
        public ActionResult Damaged()
        {
            ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName");
            return View();
        }

        // CÂU 4b: Ghi nhận hàng hỏng (POST)
        [HttpPost]
        public ActionResult Damaged(int productID, int quantityDamaged)
        {
            var product = db.Products.Find(productID);
            if (product == null || quantityDamaged <= 0 || quantityDamaged > product.Quantity)
            {
                ModelState.AddModelError("", "Số lượng hỏng không hợp lệ hoặc lớn hơn số lượng tồn kho hiện có.");
                ViewBag.ProductID = new SelectList(db.Products, "ProductID", "ProductName", productID);
                return View();
            }

            product.Quantity -= quantityDamaged;
            db.SaveChanges();
            TempData["Message"] = "Ghi nhận hàng hỏng thành công!";
            return RedirectToAction("Index");
        }

        // CÂU 5: Điều chuyển hàng giữa hai sản phẩm (GET)
        public ActionResult Transfer()
        {
            ViewBag.SourceID = new SelectList(db.Products, "ProductID", "ProductName");
            ViewBag.DestID = new SelectList(db.Products, "ProductID", "ProductName");
            return View();
        }

        // CÂU 5: Điều chuyển hàng giữa hai sản phẩm (POST)
        [HttpPost]
        public ActionResult Transfer(int sourceID, int destID, int q)
        {
            if (sourceID == destID || q <= 0)
            {
                ModelState.AddModelError("", "Sản phẩm nguồn và đích phải khác nhau và số lượng điều chuyển phải > 0.");
                ViewBag.SourceID = new SelectList(db.Products, "ProductID", "ProductName", sourceID);
                ViewBag.DestID = new SelectList(db.Products, "ProductID", "ProductName", destID);
                return View();
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var source = db.Products.Find(sourceID);
                    var dest = db.Products.Find(destID);

                    if (source.Category != dest.Category || source.Quantity < q)
                    {
                        ModelState.AddModelError("", "Hai sản phẩm phải cùng Category và sản phẩm nguồn phải đủ số lượng tồn!");
                        ViewBag.SourceID = new SelectList(db.Products, "ProductID", "ProductName", sourceID);
                        ViewBag.DestID = new SelectList(db.Products, "ProductID", "ProductName", destID);
                        return View();
                    }

                    source.Quantity -= q;
                    dest.Quantity += q;
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Message"] = "Điều chuyển hàng thành công!";
                    return RedirectToAction("Index");
                }
                catch
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình giao dịch điều chuyển.");
                }
            }

            ViewBag.SourceID = new SelectList(db.Products, "ProductID", "ProductName", sourceID);
            ViewBag.DestID = new SelectList(db.Products, "ProductID", "ProductName", destID);
            return View();
        }

        // CÂU 6: Thêm sản phẩm mới (GET)
        public ActionResult Create()
        {
            return View();
        }

        // CÂU 6: Thêm sản phẩm mới (POST)
        [HttpPost]
        public ActionResult Create(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.ProductName) || string.IsNullOrWhiteSpace(product.Category))
            {
                ModelState.AddModelError("", "Tên sản phẩm và danh mục không được để trống.");
            }
            if (product.Price <= 0)
            {
                ModelState.AddModelError("", "Giá sản phẩm phải lớn hơn 0.");
            }
            if (product.Quantity < 0)
            {
                ModelState.AddModelError("", "Số lượng phải lớn hơn hoặc bằng 0.");
            }

            if (ModelState.IsValid)
            {
                db.Products.Add(product);
                db.SaveChanges();
                TempData["Message"] = "Thêm sản phẩm mới thành công!";
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // CÂU 7: Thống kê tồn kho
        public ActionResult Statistics()
        {
            ViewBag.TotalProducts = db.Products.Count();
            ViewBag.TotalQuantity = db.Products.Sum(p => (int?)p.Quantity) ?? 0;
            ViewBag.LowStockCount = db.Products.Count(p => p.Quantity >= 1 && p.Quantity <= 4);
            return View();
        }
    }
}