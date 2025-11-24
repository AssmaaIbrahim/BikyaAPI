namespace Bikya.Services.Exceptions
{
    /// <summary>
    /// Custom exception for business logic errors.
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// Gets the error code associated with this exception.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the BusinessException class.
        /// </summary>
        /// <param name="message">The error message</param>
        public BusinessException(string message) : base(message)
        {
            ErrorCode = "BUSINESS_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the BusinessException class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="errorCode">The error code</param>
        public BusinessException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the BusinessException class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public BusinessException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = "BUSINESS_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the BusinessException class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="errorCode">The error code</param>
        /// <param name="innerException">The inner exception</param>
        public BusinessException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when a resource is not found.
    /// </summary>
    public class NotFoundException : BusinessException
    {
        public NotFoundException(string message) : base(message, "NOT_FOUND")
        {
        }

        public NotFoundException(string resourceName, object id) 
            : base($"{resourceName} with ID {id} was not found", "NOT_FOUND")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a user is not authorized to perform an action.
    /// </summary>
    public class UnauthorizedException : BusinessException
    {
        public UnauthorizedException(string message) : base(message, "UNAUTHORIZED")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a validation fails.
    /// </summary>
    public class ValidationException : BusinessException
    {
        public ValidationException(string message) : base(message, "VALIDATION_ERROR")
        {
        }
    }

    /// <summary>
    /// Exception thrown when a conflict occurs (e.g., duplicate resource).
    /// </summary>
    public class ConflictException : BusinessException
    {
        public ConflictException(string message) : base(message, "CONFLICT")
        {
        }
    }
} 