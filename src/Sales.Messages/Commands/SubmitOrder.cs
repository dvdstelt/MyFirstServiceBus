using System;

namespace Sales.Messages.Commands
{
    public class SubmitOrder
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
    }
}
