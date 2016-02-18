using DataModel.GenericRepository;

namespace DataModel.UnitOfWork
{
    public interface IUnitOfWork
    {
        #region Properties
        GenericRepository<Product> ProductRepository { get; }
        GenericRepository<User> UserRepository { get; }
        GenericRepository<Token> TokenRepository { get; } 
        #endregion
        
        #region Public methods
        /// <summary>
        /// Save method.
        /// </summary>
        void Save(); 
        #endregion
    }
}