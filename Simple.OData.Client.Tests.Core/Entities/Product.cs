﻿namespace Simple.OData.Client.Tests
{
    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int? CategoryID { get; set; }

        [NotMapped]
        public int NotMappedProperty { get; set; }
        [Column(Name = "EnglishName")]
        public string MappedEnglishName { get; set; }

        public Category Category { get; set; }

        public Product()
        {
            this.NotMappedProperty = 42;
        }
    }

    public class ExtendedProduct : Product
    {
    }
}
