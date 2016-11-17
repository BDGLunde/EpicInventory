using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Epic.Training.Project.Inventory.EventArgs;
using Epic.Training.Project.SuppliedCode.Interfaces;

namespace Epic.Training.Project.Inventory
{
    /// <summary>
    /// The item class for the C# project.  
    /// </summary>
    [Serializable]
    public class Item : IProduct
    {

        #region CONSTRUCTORS

        /// <summary>
        /// Simple constructor for Item objects
        /// </summary>
        /// <param name="name">[String] Required</param>
        /// <param name="quantity">[Int] Default == 0</param>
        /// <param name="weight">[Double] Default == 0.01</param>
        /// <param name="wholesale">[Wholesale] Default == 0.01</param>
        public Item(string name, int quantity = 0, double weight = 0.01, decimal wholesale = 0.01m)
        {
            this.Name = name;
            this.QuantityOnHand = quantity;
            this.Weight = weight;
            this.WholesalePrice = wholesale;
        }

        internal Item(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            this.Name = info.GetString("Name");
            this.QuantityOnHand = info.GetInt32("QuantityOnHand");
            this.Weight = info.GetDouble("Weight");
            this.WholesalePrice = info.GetDecimal("WholesalePrice");
        }

        public Item() : this("N/A", 1, 1, 1)
        {}

        #endregion

        #region EVENT HANDLERS AND HELPER FUNCTIONS

        /// <summary>
        /// Avoids retyping the 'Changing > set value > Changed' pattern for each property
        /// </summary>
        /// <typeparam name="T">Type of value being set. Usable by all property types.</typeparam>
        /// <param name="prop">Reference to property being set</param>
        /// <param name="value">Value being set to referenced property</param>
        /// <param name="propertyName">Name of property changing/changed</param>
        protected void SetPropNotify<T>(ref T prop, T value, [CallerMemberName] string propertyName = "") //[CallerMemberName should be the name of the Property being modified
        {
            if (!EqualityComparer<T>.Default.Equals(prop, value))
            {
                NotifyPropertyChanging(propertyName); //This is where an inventory should throw duplicate name exception
                prop = value; //ACTUAL name of item is changed here if the above does not throw an exception
                NotifyPropertyChanged(propertyName);
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(info)); }
        }

        [field: NonSerialized]
        public event PropertyChangingEventHandler PropertyChanging;
        protected void NotifyPropertyChanging(string info)
        {
            string proposedName = _proposedName; //local copy for use
            _proposedName = null; //reset instance variable back to null for safe future use

            if (PropertyChanging != null)
            {
                //PropertyChanging(this, new PropertyChangingEventArgs(info));
                PropertyChanging(this, new ExtendedChangingEventArgs(info, proposedName));
            }
        }

        #endregion

        #region PRIVATE & INTERNAL SUPPORTING FIELDS AND PUBLIC PROPERTIES

        [NonSerialized]
        internal const decimal ShippingFactor = 3.25M;
        [NonSerialized]
        internal const decimal MarkupFactor = 1.7M;

        [NonSerialized]
        private string _proposedName;
        [NonSerialized]
        internal string OldName;
        private string _name;
        /// <summary>
        /// [String] Name of the Item object. Calls PropertyChang(ing|ed) upon being set
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set 
            {
                _proposedName = value;
                SetPropNotify(ref this._name, value);
            }
        }

        [NonSerialized]
        internal int OldQuantity;
        private int _quantityOnHand;
        /// <summary>
        /// [Int] Quantity of Item available. Calls PropertyChang(ing|ed) upon being set
        /// </summary>
        public int QuantityOnHand
        {
            get
            {
                return _quantityOnHand;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(String.Format("{0} is an unsuitable value for Item Property 'QuantityOnHand'", value));
                }
                else 
                {
                    SetPropNotify(ref this._quantityOnHand, value);
                }
            }
        }

        [NonSerialized]
        internal double OldWeight;
        private double _weight; 
        /// <summary>
        /// [Double] Weight in LBS of one unit of Item. Calls PropertyChang(ing|ed) upon being set
        /// </summary>
        public double Weight
        {
            get
            {
                return _weight;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(String.Format("{0} is an unsuitable value for Item Property 'Weight'", value));
                }
                else 
                {
                    NotifyPropertyChanging("ShippingCost"); //I'M ADDING THESE TO PASS THE UNIT TEST, THEY DON'T ACTUALLY DO ANYTHING 
                    NotifyPropertyChanging("RetailPrice"); //PLEASE PROVIDE FEEDBACK. I SET THIS UP SO THAT CHANGES IN 'Weight' AND 'WholesalePrice'
                    SetPropNotify(ref this._weight, value);
                    NotifyPropertyChanged("RetailPrice"); //ULTIMATELY MAKE THE CHANGES TO AN INVENTORY'S 'TotalRetail' FIELD.
                    NotifyPropertyChanged("ShippingCost"); //DID NOT UNDERSTAND THE POINT OF RAISING CHANGING/CHANGED ON CALCULATED PROPERTIES IN THIS PARTICULAR PROJECT
                }
            }
        }

        [NonSerialized]
        internal decimal OldWholesale;
        private decimal _wholesalePrice;
        /// <summary>
        /// [Decimal] Price in USD of one unit of Item. Calls PropertyChang(ing|ed) upon being set
        /// </summary>
        public decimal WholesalePrice
        {
            get
            {
                return _wholesalePrice;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(String.Format("{0} is an unsuitable value for Item Property 'WholesalePrice'", value));
                }
                else 
                {
                    NotifyPropertyChanging("RetailPrice");
                    SetPropNotify(ref this._wholesalePrice, value);
                    NotifyPropertyChanged("RetailPrice");
                }
            }
        }

        /// <summary>
        /// [Decimal] Calculates and returns Retail Price (USD) of one unit of Item based on stored properties
        /// </summary>
        public decimal RetailPrice
        {
            get
            {
                return (Item.MarkupFactor * this._wholesalePrice) + this.ShippingCost;
            }
        }

        /// <summary>
        /// [Decimal] Calculates and returns Shipping Cost (USD) of one unit of Item based on stored properties
        /// </summary>
        public decimal ShippingCost
        {
            get
            {
                return Item.ShippingFactor * (decimal)this._weight;
            }
        }

        #endregion



        public void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("QuantityOnHand", QuantityOnHand);
            info.AddValue("Weight", Weight);
            info.AddValue("WholesalePrice", WholesalePrice);

            Type type = this.GetType();
            info.AddValue("TypeObj", type);
        }

    }
}
