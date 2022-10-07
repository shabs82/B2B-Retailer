using System;
using Messages;
using System.Threading;
using EasyNetQ;
using System.Text;

namespace Customer
{
    public class Customer
    {
        private int customerID;
        private int productID;
        private string country;

        public Customer(int customerID, int productID, string country)
        {
            this.customerID = customerID;
            this.productID = productID;
            this.country = country;
        }

        public void Start()
        {
            lock (this)
            {
                Console.WriteLine("Customer " + customerID + " is running. Waiting for a reply.");
            }

            OrderRequestMessage request = new OrderRequestMessage
            {
                CustomerId = customerID,
                ProductId = productID,
                Country = country
            };

            using (IBus bus = RabbitHutch.CreateBus("host= amqps://ruvyiqnd:04qvfFXGSowhSjDftIkWiEI6_s_Fn7kT@rattlesnake.rmq.cloudamqp.com/ruvyiqnd ;persistentMessages=false"))
            {
                // Listen to reply messages from the Retailer (use Topic Based Routing).
                bus.PubSub.Subscribe<OrderReplyMessage>("customer" + customerID,
                HandleOrderEvent, x => x.WithTopic(customerID.ToString()));

                // Send an order request message to the Retailer (use a point-to-point channel).
                bus.SendReceive.Send<OrderRequestMessage>("customerToRetailerQueue", request);

                // Block this thread so that the customer instance will not exit.
                Console.ReadLine();
            }
        }


        private void HandleOrderEvent(OrderReplyMessage message)
        {
            StringBuilder reply = new StringBuilder();
            reply.Append("Order reply received by customer:" + customerID + "\n");
            reply.Append("Warehouse Id: " + message.WarehouseId + "\n");
            reply.Append("Order Id: " + message.OrderId + "\n");
            reply.Append("Items in stock: " + message.ItemsInStock + "\n");
            reply.Append("Shipping charge: " + message.ShippingCharge + "\n");
            reply.Append("Days for delivery: " + message.DaysForDelivery + "\n");

            lock (this)
            {
                Console.WriteLine(reply.ToString());
            }
        }

    }
}
