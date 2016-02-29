#region Using namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Hosting;
using BusinessEntities;
using BusinessServices;
using DataModel;
using DataModel.GenericRepository;
using DataModel.UnitOfWork;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;
using TestsHelper;
using WebApi.Controllers;
using WebApi.ErrorHelper; 
#endregion

namespace ApiController.Tests
{
    [TestFixture]
    public class ProductControllerTest
    {

        #region Variables

        private IProductServices _productService;
        private ITokenServices _tokenService;
        private IUnitOfWork _unitOfWork;
        private List<Product> _products;
        private List<Token> _tokens;
        private GenericRepository<Product> _productRepository;
        private GenericRepository<Token> _tokenRepository;
        private WebApiDbEntities _dbEntities;
        private HttpClient _client;

        private HttpResponseMessage _response;
        private string _token;
        private const string ServiceBaseURL = "http://localhost:50875/";

        #endregion

        #region Test fixture setup

        /// <summary>
        /// Initial setup for tests
        /// </summary>
        [TestFixtureSetUp]
        public void Setup()
        {
            _products = SetUpProducts();
            _tokens = SetUpTokens();
            _dbEntities = new Mock<WebApiDbEntities>().Object;
            _tokenRepository = SetUpTokenRepository();
            _productRepository = SetUpProductRepository();
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.SetupGet(s => s.ProductRepository).Returns(_productRepository);
            unitOfWork.SetupGet(s => s.TokenRepository).Returns(_tokenRepository);
            _unitOfWork = unitOfWork.Object;
            _productService = new ProductServices(_unitOfWork);
            _tokenService = new TokenServices(_unitOfWork);
            _client = new HttpClient { BaseAddress = new Uri(ServiceBaseURL) };
            var tokenEntity = _tokenService.GenerateToken(1);
            _token = tokenEntity.AuthToken;
            _client.DefaultRequestHeaders.Add("Token", _token);
        }

        #endregion

        #region Setup
        /// <summary>
        /// Re-initializes test.
        /// </summary>
        [SetUp]
        public void ReInitializeTest()
        {
            _client = new HttpClient { BaseAddress = new Uri(ServiceBaseURL) };
            _client.DefaultRequestHeaders.Add("Token", _token);
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
        /// Setup dummy repository
        /// </summary>
        /// <returns></returns>
        private GenericRepository<Token> SetUpTokenRepository()
        {
            // Initialise repository
            var mockRepo = new Mock<GenericRepository<Token>>(MockBehavior.Default, _dbEntities);

            // Setup mocking behavior
            mockRepo.Setup(p => p.GetAll()).Returns(_tokens);

            mockRepo.Setup(p => p.GetByID(It.IsAny<int>()))
                .Returns(new Func<int, Token>(
                             id => _tokens.Find(p => p.TokenId.Equals(id))));

            mockRepo.Setup(p => p.Insert((It.IsAny<Token>())))
                .Callback(new Action<Token>(newToken =>
                {
                    dynamic maxTokenID = _tokens.Last().TokenId;
                    dynamic nextTokenID = maxTokenID + 1;
                    newToken.TokenId = nextTokenID;
                    _tokens.Add(newToken);
                }));

            mockRepo.Setup(p => p.Update(It.IsAny<Token>()))
                .Callback(new Action<Token>(token =>
                {
                    var oldToken = _tokens.Find(a => a.TokenId == token.TokenId);
                    oldToken = token;
                }));

            mockRepo.Setup(p => p.Delete(It.IsAny<Token>()))
                .Callback(new Action<Token>(prod =>
                {
                    var tokenToRemove =
                        _tokens.Find(a => a.TokenId == prod.TokenId);

                    if (tokenToRemove != null)
                        _tokens.Remove(tokenToRemove);
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

        /// <summary>
        /// Setup dummy tokens data
        /// </summary>
        /// <returns></returns>
        private static List<Token> SetUpTokens()
        {
            var tokId = new int();
            var tokens = DataInitializer.GetAllTokens();
            foreach (Token tok in tokens)
                tok.TokenId = ++tokId;
            return tokens;
        }

        #endregion

        #region Unit Tests

        /// <summary>
        /// Get all products test
        /// </summary>
        [Test]
        public void GetAllProductsTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/all")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            _response = productController.Get();

            var responseResult = JsonConvert.DeserializeObject<List<Product>>(_response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(_response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(responseResult.Any(), true);
            var comparer = new ProductComparer();
            CollectionAssert.AreEqual(
                responseResult.OrderBy(product => product, comparer),
                _products.OrderBy(product => product, comparer), comparer);
        }

        /// <summary>
        /// Get product by Id
        /// </summary>
        [Test]
        public void GetProductByIdTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/productid/2")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            _response = productController.Get(2);

            var responseResult = JsonConvert.DeserializeObject<Product>(_response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(_response.StatusCode, HttpStatusCode.OK);
            AssertObjects.PropertyValuesAreEquals(responseResult,
                                                    _products.Find(a => a.ProductName.Contains("Mobile")));
        }

        /// <summary>
        /// Get product by wrong Id
        /// </summary>
        [Test]
        //[ExpectedException("WebApi.ErrorHelper.ApiDataException")]
        public void GetProductByWrongIdTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/productid/10")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            var ex = Assert.Throws<ApiDataException>(() => productController.Get(10));
            Assert.That(ex.ErrorCode,Is.EqualTo(1001));
            Assert.That(ex.ErrorDescription, Is.EqualTo("No product found for this id."));

        }

        /// <summary>
        /// Get product by invalid Id
        /// </summary>
        [Test]
       // [ExpectedException("WebApi.ErrorHelper.ApiException")]
        public void GetProductByInvalidIdTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/productid/-1")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            var ex = Assert.Throws<ApiException>(() => productController.Get(-1));
            Assert.That(ex.ErrorCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(ex.ErrorDescription, Is.EqualTo("Bad Request..."));
        }

        /// <summary>
        /// Create product test
        /// </summary>
        [Test]
        public void CreateProductTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/Create")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            var newProduct = new ProductEntity()
            {
                ProductName = "Android Phone"
            };

            var maxProductIDBeforeAdd = _products.Max(a => a.ProductId);
            newProduct.ProductId = maxProductIDBeforeAdd + 1;
            productController.Post(newProduct);
            var addedproduct = new Product() { ProductName = newProduct.ProductName, ProductId = newProduct.ProductId };
            AssertObjects.PropertyValuesAreEquals(addedproduct, _products.Last());
            Assert.That(maxProductIDBeforeAdd + 1, Is.EqualTo(_products.Last().ProductId));
        }

        /// <summary>
        /// Update product test
        /// </summary>
        [Test]
        public void UpdateProductTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/Modify")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            var firstProduct = _products.First();
            firstProduct.ProductName = "Laptop updated";
            var updatedProduct = new ProductEntity() { ProductName = firstProduct.ProductName, ProductId = firstProduct.ProductId };
            productController.Put(firstProduct.ProductId, updatedProduct);
            Assert.That(firstProduct.ProductId, Is.EqualTo(1)); // hasn't changed
        }

        /// <summary>
        /// Delete product test
        /// </summary>
        [Test]
        public void DeleteProductTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/Remove")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            int maxID = _products.Max(a => a.ProductId); // Before removal
            var lastProduct = _products.Last();

            // Remove last Product
            productController.Delete(lastProduct.ProductId);
            Assert.That(maxID, Is.GreaterThan(_products.Max(a => a.ProductId))); // Max id reduced by 1
        }

        /// <summary>
        /// Delete product test with invalid id
        /// </summary>
        [Test]
        public void DeleteProductInvalidIdTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/remove")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            var ex = Assert.Throws<ApiException>(() => productController.Delete(-1));
            Assert.That(ex.ErrorCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
            Assert.That(ex.ErrorDescription, Is.EqualTo("Bad Request..."));
        }

        /// <summary>
        /// Delete product test with wrong id
        /// </summary>
        [Test]
        public void DeleteProductWrongIdTest()
        {
            var productController = new ProductController(_productService)
            {
                Request = new HttpRequestMessage
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri(ServiceBaseURL + "v1/Products/Product/remove")
                }
            };
            productController.Request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());

            int maxID = _products.Max(a => a.ProductId); // Before removal

            var ex = Assert.Throws<ApiDataException>(() => productController.Delete(maxID+1));
            Assert.That(ex.ErrorCode, Is.EqualTo(1002));
            Assert.That(ex.ErrorDescription, Is.EqualTo("Product is already deleted or not exist in system."));
        }
      
        #endregion

        #region Integration Test

        /// <summary>
        /// Get all products test
        /// </summary>
        [Test]
        public void GetAllProductsIntegrationTest()
        {
            #region To be written inside Setup method specifically for integration tests
            var client = new HttpClient { BaseAddress = new Uri(ServiceBaseURL) };
            client.DefaultRequestHeaders.Add("Authorization", "Basic YWtoaWw6YWtoaWw=");
            MediaTypeFormatter jsonFormatter = new JsonMediaTypeFormatter();
            _response = client.PostAsync("login", null).Result;

            if (_response != null && _response.Headers != null && _response.Headers.Contains("Token") && _response.Headers.GetValues("Token") != null)
            {
                client.DefaultRequestHeaders.Clear();
                _token = ((string[])(_response.Headers.GetValues("Token")))[0];
                client.DefaultRequestHeaders.Add("Token", _token);
            } 
            #endregion

            _response = client.GetAsync("v1/Products/Product/allproducts/").Result;
            var responseResult =
                JsonConvert.DeserializeObject<List<ProductEntity>>(_response.Content.ReadAsStringAsync().Result);
            Assert.AreEqual(_response.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(responseResult.Any(), true);
        }

        #endregion

        #region Tear Down
        /// <summary>
        /// Tears down each test data
        /// </summary>
        [TearDown]
        public void DisposeTest()
        {
            if (_response != null)
                _response.Dispose();
            if (_client != null)
                _client.Dispose();
        }

        #endregion

        #region TestFixture TearDown.

        /// <summary>
        /// TestFixture teardown
        /// </summary>
        [TestFixtureTearDown]
        public void DisposeAllObjects()
        {
            _tokenService = null;
            _productService = null;
            _unitOfWork = null;
            _tokenRepository = null;
            _productRepository = null;
            _tokens = null;
            _products = null;
            if (_response != null)
                _response.Dispose();
            if (_client != null)
                _client.Dispose();
        }

        #endregion
    }
}
