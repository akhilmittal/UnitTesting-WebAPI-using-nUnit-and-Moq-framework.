#region using namespaces.
using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using BusinessEntities;
using DataModel;
using DataModel.GenericRepository;
using DataModel.UnitOfWork;
using Moq;
using NUnit.Framework;
using TestsHelper;

#endregion

namespace BusinessServices.Tests
{
    /// <summary>
    /// Product Service Test
    /// </summary>
    public class ProductServicesTest
    {
        #region Variables

        private IProductServices _productService;
        private IUnitOfWork _unitOfWork;
        private List<Product> _products;
        private GenericRepository<Product> _productRepository;
        private WebApiDbEntities _dbEntities;
        #endregion

        #region Test fixture setup

        /// <summary>
        /// Initial setup for tests
        /// </summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            _products = SetUpProducts();
        }

        #endregion

        #region Setup

        /// <summary>
        /// Re-initializes test.
        /// </summary>
        [SetUp]
        public void ReInitializeTest()
        {
            _products = SetUpProducts();
            _dbEntities = new Mock<WebApiDbEntities>().Object;
            _productRepository = SetUpProductRepository();
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.SetupGet(s => s.ProductRepository).Returns(_productRepository);
            _unitOfWork = unitOfWork.Object;
            _productService = new ProductServices(_unitOfWork);
            
        }

        #endregion

        #region Private member methods

        /// <summary>
        /// Setup dummy repository
        /// </summary>
        /// <returns></returns>
        private GenericRepository<Product> SetUpProductRepository()
        {
            // Initialise repository
            var mockRepo = new Mock<GenericRepository<Product>>(MockBehavior.Default, _dbEntities);

            // Setup mocking behavior
            mockRepo.Setup(p => p.GetAll()).Returns(_products);

            mockRepo.Setup(p => p.GetByID(It.IsAny<int>()))
                .Returns(new Func<int, Product>(
                             id => _products.Find(p => p.ProductId.Equals(id))));

            mockRepo.Setup(p => p.Insert((It.IsAny<Product>())))
                .Callback(new Action<Product>(newProduct =>
                                                  {
                                                      dynamic maxProductID = _products.Last().ProductId;
                                                      dynamic nextProductID = maxProductID + 1;
                                                      newProduct.ProductId = nextProductID;
                                                      _products.Add(newProduct);
                                                  }));

            mockRepo.Setup(p => p.Update(It.IsAny<Product>()))
                .Callback(new Action<Product>(prod =>
                                                  {
                                                      var oldProduct = _products.Find(a => a.ProductId == prod.ProductId);
                                                      oldProduct = prod;
                                                  }));

            mockRepo.Setup(p => p.Delete(It.IsAny<Product>()))
                .Callback(new Action<Product>(prod =>
                                                  {
                                                      var productToRemove =
                                                          _products.Find(a => a.ProductId == prod.ProductId);

                                                      if (productToRemove != null)
                                                          _products.Remove(productToRemove);
                                                  }));

            // Return mock implementation object
            return mockRepo.Object;
        }

        /// <summary>
        /// Setup dummy products data
        /// </summary>
        /// <returns></returns>
        private static List<Product> SetUpProducts()
        {
            var prodId = new int();
            var products = DataInitializer.GetAllProducts();
            foreach (Product prod in products)
                prod.ProductId = ++prodId;
            return products;

        }

        #endregion

        #region Unit Tests

        /// <summary>
        /// Service should return all the products
        /// </summary>
        [Test]
        public void GetAllProductsTest()
        {
            var products = _productService.GetAllProducts();
            if (products != null)
            {
                var productList =
                    products.Select(
                        productEntity =>
                        new Product {ProductId = productEntity.ProductId, ProductName = productEntity.ProductName}).
                        ToList();
                var comparer = new ProductComparer();
                CollectionAssert.AreEqual(
                    productList.OrderBy(product => product, comparer),
                    _products.OrderBy(product => product, comparer), comparer);
            }
        }

        /// <summary>
        /// Service should return null
        /// </summary>
        [Test]
        public void GetAllProductsTestForNull()
        {
            _products.Clear();
            var products = _productService.GetAllProducts();
            Assert.Null(products);
            SetUpProducts();
        }

        /// <summary>
        /// Service should return product if correct id is supplied
        /// </summary>
        [Test]
        public void GetProductByRightIdTest()
        {
            var mobileProduct = _productService.GetProductById(2);
            if (mobileProduct != null)
            {
                Mapper.CreateMap<ProductEntity, Product>();
                var productModel = Mapper.Map<ProductEntity, Product>(mobileProduct);
                AssertObjects.PropertyValuesAreEquals(productModel,
                                                      _products.Find(a => a.ProductName.Contains("Mobile")));
            }
        }

        /// <summary>
        /// Service should return null
        /// </summary>
        [Test]
        public void GetProductByWrongIdTest()
        {
            var product = _productService.GetProductById(0);
            Assert.Null(product);
        }

        /// <summary>
        /// Add new product test
        /// </summary>
        [Test]
        public void AddNewProductTest()
        {
            var newProduct = new ProductEntity()
                                 {
                                     ProductName = "Android Phone"
                                 };

            var maxProductIDBeforeAdd = _products.Max(a => a.ProductId);
            newProduct.ProductId = maxProductIDBeforeAdd + 1;
            _productService.CreateProduct(newProduct);
            var addedproduct = new Product() {ProductName = newProduct.ProductName, ProductId = newProduct.ProductId};
            AssertObjects.PropertyValuesAreEquals(addedproduct, _products.Last());
            Assert.That(maxProductIDBeforeAdd + 1, Is.EqualTo(_products.Last().ProductId));
        }

        /// <summary>
        /// Update product test
        /// </summary>
        [Test]
        public void UpdateProductTest()
        {
            var firstProduct = _products.First();
            firstProduct.ProductName = "Laptop updated";
            var updatedProduct = new ProductEntity()
                                     {ProductName = firstProduct.ProductName, ProductId = firstProduct.ProductId};
            _productService.UpdateProduct(firstProduct.ProductId, updatedProduct);
            Assert.That(firstProduct.ProductId, Is.EqualTo(1)); // hasn't changed
            Assert.That(firstProduct.ProductName, Is.EqualTo("Laptop updated")); // Product name changed
        }

        /// <summary>
        /// Delete product test
        /// </summary>
        [Test]
        public void DeleteProductTest()
        {
            int maxID = _products.Max(a => a.ProductId); // Before removal
            var lastProduct = _products.Last();

            // Remove last Product
            _productService.DeleteProduct(lastProduct.ProductId);
            Assert.That(maxID, Is.GreaterThan(_products.Max(a => a.ProductId))); // Max id reduced by 1
        }

        #endregion


        #region Tear Down

        /// <summary>
        /// Tears down each test data
        /// </summary>
        [TearDown]
        public void DisposeTest()
        {
            _productService = null;
            _unitOfWork = null;
            _productRepository = null;
            if (_dbEntities != null)
                _dbEntities.Dispose();
            _products = null;
        }

        #endregion

        #region TestFixture TearDown.

        /// <summary>
        /// TestFixture teardown
        /// </summary>
        [TestFixtureTearDown]
        public void DisposeAllObjects()
        {
            _products = null;
        }

        #endregion
    }
}
