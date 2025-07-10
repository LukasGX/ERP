using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Fix
{
    class Test
    {
        public void Main()
        {
            Program program = new Program();

            // storage slots
            program.NewStorageSlot();
            program.NewStorageSlot();
            program.NewStorageSlot();
            program.ListStorageSlots();
            // article types
            ArticleType typeMetal = program.NewArticleType("Metal");
            ArticleType typeWood = program.NewArticleType("Wood");
            ArticleType typeHammer = program.NewArticleType("Hammer");
            // articles (stocks)
            Article metal = program.NewArticle(0, 6000);
            Article wood = program.NewArticle(1, 6000);
            Article hammer = program.NewArticle(2, 0);
            // sort
            program.SortArticle(0, 0);
            program.SortArticle(1, 1);
            program.SortArticle(2, 2);

            program.DisplayInventory();

            List<Article> toOrder = new List<Article>
            {
                program.NewArticle(2, 6, false)
            };
            program.NewOrder(toOrder);
            program.ListOrders();

            Prices prices = program.NewPrices(new Dictionary<ArticleType, double>()
            {
                { typeMetal, 3.0 },
                { typeWood, 2.3 },
                { typeHammer, 12.0 }
            });

            ExecuteOrders(program, prices);
            program.ListOrders();
            program.DisplayInventory();

            program.ListBills();
        }

        private void ProduceHammer(Program program, int amount)
        {
            program.WithdrawArticle(0, amount);
            program.WithdrawArticle(1, amount);
            program.RestockArticle(2, amount);
        }

        private void ExecuteOrders(Program program, Prices prices)
        {
            Order newestOrder = program.NewestOrder();
            foreach (Article item in newestOrder.Articles)
            {
                ProduceHammer(program, item.Stock);
                program.WithdrawArticle(2, item.Stock);
            }
            program.NewBill(newestOrder, prices);
            program.FinishOrder(newestOrder);
        }
    }
}