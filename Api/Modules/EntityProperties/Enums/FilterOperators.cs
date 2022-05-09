namespace Api.Modules.EntityProperties.Enums
{
    /// <summary>
    /// An enum containing all possible operators for filters.
    /// </summary>
    public enum FilterOperators
    {
        /// <summary>
        /// The value should be the same as the other value.
        /// </summary>
        Equals,
        
        /// <summary>
        /// The value should be different from the other value.
        /// </summary>
        NotEquals,
        
        /// <summary>
        /// The value should contain the other value.
        /// </summary>
        Contains,
        
        /// <summary>
        /// The value should not contain the other value.
        /// </summary>
        DoesNotContain,
        
        /// <summary>
        /// The value should start with the other value.
        /// </summary>
        StartsWith,
        
        /// <summary>
        /// The value should not start with the other value.
        /// </summary>
        DoesNotStartWith,
        
        /// <summary>
        /// The value should end with the other value.
        /// </summary>
        EndsWith,
        
        /// <summary>
        /// The value should not end with the other value.
        /// </summary>
        DoesNotEndWith,
        
        /// <summary>
        /// The value should be empty or null.
        /// </summary>
        IsEmpty,
        
        /// <summary>
        /// The value should not be empty and not be null.
        /// </summary>
        IsNotEmpty,
        
        /// <summary>
        /// The value should be greater than or equal to the other value.
        /// </summary>
        GreaterThanOrEqualTo,
        
        /// <summary>
        /// The value should be greater than the other value.
        /// </summary>
        GreaterThan,
        
        /// <summary>
        /// The value should be less than or equal to the other value.
        /// </summary>
        LessThanOrEqualTo,
        
        /// <summary>
        /// The value should be less than the other value.
        /// </summary>
        LessThan
    }
}
