using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AttributeRouting;
using AttributeRouting.Web.Http;
using BusinessEntities;
using BusinessServices;
using WebApi.ActionFilters;
using WebApi.ErrorHelper;

namespace WebApi.Controllers
{
    [AuthorizationRequired]
    [RoutePrefix("v1/Products/Product")]
    public class ProductController : ApiController
    {
        #region Private variable.

        private readonly IProductServices _productServices;

        #endregion

        #region Public Constructor

        /// <summary>
        /// Public constructor to initialize product service instance
        /// </summary>
        public ProductController(IProductServices productServices)
        {
            _productServices = productServices;
        }

        #endregion

        // GET api/product
        [GET("allproducts")]
        [GET("all")]
        public HttpResponseMessage Get()
        {
            var products = _productServices.GetAllProducts();
            var productEntities = products as List<ProductEntity> ?? products.ToList();
            if (productEntities.Any())
                return Request.CreateResponse(HttpStatusCode.OK, productEntities);
            throw new ApiDataException(1000, "Products not found", HttpStatusCode.NotFound);
        }

        // GET api/product/5
        [GET("productid/{id?}")]
        [GET("particularproduct/{id?}")]
        [GET("myproduct/{id:range(1, 3)}")]
        public HttpResponseMessage Get(int id)
        {
            if (id > 0)
            {
                var product = _productServices.GetProductById(id);
                if (product != null)
                    return Request.CreateResponse(HttpStatusCode.OK, product);

                throw new ApiDataException(1001, "No product found for this id.", HttpStatusCode.NotFound);
            }
            throw new ApiException() { ErrorCode = (int)HttpStatusCode.BadRequest, ErrorDescription = "Bad Request..." };
        }

        // POST api/product
        [POST("Create")]
        [POST("Register")]
        public int Post([FromBody] ProductEntity productEntity)
        {
            return _productServices.CreateProduct(productEntity);
        }

        // PUT api/product/5
        [PUT("Update/productid/{id}")]
        [PUT("Modify/productid/{id}")]
        public bool Put(int id, [FromBody] ProductEntity productEntity)
        {
            return id > 0 && _productServices.UpdateProduct(id, productEntity);
        }

        // DELETE api/product/5
        [DELETE("remove/productid/{id}")]
        [DELETE("clear/productid/{id}")]
        [PUT("delete/productid/{id}")]
        public bool Delete(int id)
        {
            if (id > 0)
            {
                var isSuccess = _productServices.DeleteProduct(id);
                if (isSuccess)
                {
                    return true;
                }
                throw new ApiDataException(1002, "Product is already deleted or not exist in system.", HttpStatusCode.NoContent );
            }
            throw new ApiException() {ErrorCode = (int) HttpStatusCode.BadRequest, ErrorDescription = "Bad Request..."};
        }
    }
}
