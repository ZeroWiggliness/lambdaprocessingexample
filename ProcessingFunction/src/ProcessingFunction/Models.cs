using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessingFunction
{
    public class Client
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid Reference { get; set; }
        public decimal TaxFreeAllowance { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public int AccountNumber { get; set; }
        public decimal CashBalance { get; set; }
        public string Currency { get; set; }
        public decimal TaxesPaid { get; set; }
    }

    public class Portfolio
    {
        public int Id { get; set; }
        public int AccountNumber { get; set; }
        public Guid PortfolioReference { get; set; }
        public Guid ClientReference { get; set; }
        public string AgentCode { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public int AccountNumber { get; set; }
        public string TransactionReference { get; set; }
        public decimal Amount { get; set; }
        public string Keyword { get; set; }
    }

    public class Message
    {
        public string Type { get; set; }
    }

    public class PortfolioMessage : Message
    {
        public PortfolioMessage()
        {
            Type = "portfolio_message";
        }

        public Guid PortfolioReference { get; set; }
        public decimal CashBalance { get; set; }
        public decimal SumOfDeposits { get; set; }
        public int NumberOfTransactions { get; set; }
    }

    public class ClientMessage : Message
    {
        public ClientMessage()
        {
            Type = "client_message";
        }

        public Guid ClientReference { get; set; }
        public decimal TaxFreeAllowance { get; set; }
        public decimal TaxesPaid { get; set; }
    }

    public class ErrorMessage : Message
    {
        public ErrorMessage()
        {
            Type = "error_message";
            Message = "";
        }

        public Guid? PortfolioReference { get; set; }
        public Guid? ClientReference { get; set; }
        public string Message { get; set; }
    }
}
