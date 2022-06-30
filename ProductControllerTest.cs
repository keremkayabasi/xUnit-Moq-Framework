using Microsoft.AspNetCore.Mvc;
using Moq;
using RealWorldUnitTest.Web.Controllers;
using RealWorldUnitTest.Web.Models;
using RealWorldUnitTest.Web.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealWorldUnitTest.Test
{
    
    public class ProductControllerTest
    {
        private readonly Mock<IRepository<Product>> _mockRepo;
        private readonly ProductsController _controller;
        private List<Product> products;
        public ProductControllerTest()
        {
            _mockRepo=new Mock<IRepository<Product>>();
            _controller=new ProductsController(_mockRepo.Object);
            products=new List<Product>() { 
                new Product { Id=1, Name="Kalem",Price=100,Stock=50,Color="Kırmızı"}, 
                new Product { Id = 2, Name = "Defter", Price = 220, Stock = 500, Color = "Mavi" } 
            };
        }
        [Fact]
        public async void Index_ActionExecutes_ReturnView()
        {
            //view result kontrol edildiği için mock ile ilgili birşey yapmıyoruz.
            var result=await _controller.Index();
            Assert.IsType<ViewResult>(result);

        }

        [Fact]
        public async void Index_ActionExecutes_ReturnProductList()
        {
            _mockRepo.Setup(repo => repo.GetAll()).ReturnsAsync(products);
            var result = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var productList=Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);

            Assert.Equal<int>(2, productList.Count());
        }
        [Fact]
        public async void Details_IdNull_ReturnRedirectToIndexAction()
        {
            var result = await _controller.Details(null);
            var redirect=Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async void Details_InvalidId_ReturnNotFound()
        {
            Product product = null;
            _mockRepo.Setup(x => x.GetById(0)).ReturnsAsync(product);
            var result =await _controller.Details(0);

            var redirect = Assert.IsType<NotFoundResult>(result);
            Assert.Equal<int>(404, redirect.StatusCode);
        }


        [Theory]
        [InlineData(1)]
        public async void Details_ValidId_ReturnsProduct(int productId)
        {
            Product product = products.First(x => x.Id == productId);
            _mockRepo.Setup(x=>x.GetById(productId)).ReturnsAsync(product);
            var result=await _controller.Details(productId);
            var viewResult=Assert.IsType<ViewResult>(result);
            var resultProduct=Assert.IsAssignableFrom<Product>(viewResult.Model);
            Assert.Equal(product.Id, resultProduct.Id);
            Assert.Equal(product.Name, resultProduct.Name);

        }

        //Create işlemi başarılıysa geriye view dönüp dönmediğini kontrol edelim.
        [Fact]
        public void Create_ActionExecutes_ReturnView()
        {
            var result = _controller.Create();
            Assert.IsType<ViewResult>(result);
        }


        //modelimiz hatalıysa geriye view dönüp dönmediğini kontrol edelim.
        [Fact]
        public async void CreatePost_InValidModelState_ReturnView()
        {
            _controller.ModelState.AddModelError("Name", "Name alanı gerekldir.");
            var result = await _controller.Create(products.First());
            //gelen result ViewResult'mı onu kontrol ettik.
            var viewResult =Assert.IsType<ViewResult>(result);
            //viewResult'un modeli product mı onu kontrol ediyoruz.Çunkü geriye product nesnesi dönmesi lazım. 
            Assert.IsType<Product>(viewResult.Model);
        }

        //create metotunun if(ModelState.IsValid) bloğu içerisindeki repository'i Create metodunu test etmiyoruz. sadece ModelState Valid olursa index sayfasına dönüp dönmediğini test edeceğiz.

        [Fact]
        public async void CreatePost_InValidModelState_ReturnRedirectToIndexAction()
        {
            var result = await _controller.Create(products.First());
            var redirect=Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index",redirect.ActionName);
        }


        //model başarılıysa yani geçerliyse create metodunun çalışıp çalışmadığını test edelim.
        [Fact]
        public async void CreatePost_ValidModelState_CreateMethodExecute()
        {
            Product newProduct = null;
            
            _mockRepo.Setup(repo => repo.Create(It.IsAny<Product>())).Callback<Product>(x => newProduct = x);
            //Create simule işlemi sonrası CallBack çalıştırılır ve It.IsAny ile Create den gelen herhangi bir product'ı newProduct'a eşitledik. 
            var result = await _controller.Create(products.First());

            //doğrulama yapıyoruz acaba create metodu çalıştı mı çalışmadı mı
            _mockRepo.Verify(repo=>repo.Create(It.IsAny<Product>()),Times.Once());

            Assert.Equal(products.First().Id, newProduct.Id);

        }

        //model state geçersiz olduğunda create methodunun çalışmamasını simule edelim.
        [Fact]
        public async void CreatePOST_InValidModelState_NeverCreateMethodExecute()
        {
            //1.senaryo controllerdakini kontrol ettim
            //önce modelState üzerinden bir hata oluşturualım.
            //_controller.ModelState.AddModelError("Name", "hata");
            //var result = await _controller.Create(products.First());
            // Assert.Null(result);


            //2.senaryo mockRepo verify ile kontrol ettik.
            //önce model state'in yanlış olduğunu göstermek için hata oluşturduk.
            _controller.ModelState.AddModelError("Name", "isim alanı boş geçilmez");
            var result = await _controller.Create(products.First());
            _mockRepo.Verify(repo=>repo.Create(It.IsAny<Product>()),Times.Never());


        }

        //bu methodla yaptığımız test'in başarılı olup olmadığını görebilmek için GET olan Edit methodu içerisindeki if(id==null) bloğu içerisindeki return satırını NotFound(); şeklinde değiştirerek RedirectToActionResult yerine NotFoundResult geldiğini görebiliriz. 
        [Fact]        
        public async void Edit_IdNull_ReturnRedirectToIndexAction()
        {
            var result = await _controller.Edit(null);
            var redirect=Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            //Equal ile de karşılaştırma yaparak gelen RedirectToActionResult'ın ActionName'i Index mi yani doğru yere mi yönlendirdik onu kontrol etmiş olduk.
        }


        //Edit methodunun 2. testi veritabanında olmayan bir id verdiğimizde ne olacak NotFound dönecek mi onu kontrol edeceğiz.
        //method'da repository'e gittiği için Mock nesnesi oluşturmamız gerekiyor.
        [Theory]
        [InlineData(1)]//olmayan bir data verdik. yukarıda 1 ve 2 id'li product'lar eklediğim için 3 veririz böyle bir id'li product yok. 
        public async void Edit_IdInValid_ReturnNotFound(int productId)
        {
            Product product = null;
            _mockRepo.Setup(x => x.GetById(productId)).ReturnsAsync(product);
            var result = await _controller.Edit(productId);
            var redirect=Assert.IsType<NotFoundResult>(result);
            Assert.Equal<int>(404,redirect.StatusCode);
        
        }

        //burada edit fonksiyonunun 2. if bloğunu test edicez. diyelim ki id null değil ve veritabanında böyle bir product var. bu durumda bakalım geriye product dönüyor mu.
        [Theory,InlineData(1)]
        public async void Edit_ActionExecute_ReturnProduct(int productId)
        {
            var product = products.First(x => x.Id == productId);
            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);
            var result=await _controller.Edit(productId);
            var viewResult = Assert.IsType<ViewResult>(result);// burada result'u kontrol ediyoruz. ViewResult mı diye.
            var resultProduct = Assert.IsAssignableFrom<Product>(viewResult.Model);
            //datayı almaya çalışalım. //IsAssignableFrom ile miras alıp almadığı bile kontrol edilebilir.
            Assert.Equal(product.Id, resultProduct.Id);
            Assert.Equal(product.Name, resultProduct.Name);

        }
        
        
        
        //EDİT METHOD'UNUN POST ATTİRUBÜTÜNÜ TEST EDECEĞİZ. YANİ KULLANICI BİR GÜNCELLEME EDİT İŞLEMİ YAPTIKTAN SONRA, BUTONA BASTIKTAN SONRA ÇALIŞACAK OLAN METHOD.

        //Edit Post methodunun ilk if bloğunu test ediyoruz.

        [Theory,InlineData(1)]
        public void EditPOST_IdIsNotEqualProduct_ReturnNotFound(int productId)
        {
            //_repository olmadığı için kodlarda mocklama yapmayacağız.
            //Edit metodu içerisinde inline datada gönderdiğimizden farklı bir id gönderdik. amacımız gerçekte böyle bir id yoksa not found verip vermediğini görmek. InlineData da 2 versek, controllerdaki ilk if bloğundan çıkıp ikinci bloğa gireceği için test false döner.
            var result = _controller.Edit(2,products.First(x=>x.Id==productId));
            var redirect = Assert.IsType<NotFoundResult>(result);

        }



        //Edit Post Methodunun ikinci if bloğunu modelState geçerli ise update işlemi gerçekleşecek ve sonrasında index sayfasına yönlendirecek. Ancak biz hatalı bir modelState hatalıysa örneğin name alanını boş gönderirsek yine edit sayfasına dönmemiz lazım ve arkasındanda edit sayfasına gönderdiğimiz product nesnesini tekrar view ile beraber cs.html tarafına gönderiyoruz. bu durumu test edeceğiz. bir hata olduğu zaman gönderdiğimiz product nesnesini modelde alabilecekmiyiz bunu göreceğiz. Hem viewResult durumu kontrol edeceğiz hemde  View'in bir product'ı olup olmadığını test etmiş olacağız.


        [Theory,InlineData(1)]
        public void EditPOST_InValidModelState_ReturnView(int productId)
        {
            //önce bir hata oluşturmamız lazım.
            _controller.ModelState.AddModelError("Name", "Name alanı boş olamaz");
            var result = _controller.Edit(productId, products.First(x => x.Id == productId));
            //burada hem productId ile vermiş olduğumuz id'yi veriyoruz. hemde product olarak vermiş olduğumuz productId'ye sahip ürünü verdik. 

            var viewResult = Assert.IsType<ViewResult>(result);//Result'ın ViewResult olup olmadığını kontrol edelim. 
            Assert.IsType<Product>(viewResult.Model); //Gelen modelin product nesnesi olup olmadığını kontrol edelim.
            

        }




        //ModelState geçerli olduğu zaman index sayfasına dönüp dönmediğini test edelim. O yüzden update ile ilgili bir mocklama yapmayacağız.

        [Theory, InlineData(1)]
        public void EditPOST_ValidModelState_ReturnRedirectToIndex(int productId)
        {
            var result=_controller.Edit(productId,products.First(x=>x.Id==productId));
            //product id'si 1 olan ürünü verdik.
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            //gelen result'un RedirectionToActionResult mu değil mi tipini kontrol ettik.

            Assert.Equal("Index", redirect.ActionName);
                //bunun index' bir RedirectionToActionResult olacağını test edelim.
        }


        //ModelState geçerli olduğu zaman update butonu çalışıp çalışmayacağını test edelim. _repository olduğu için mocklama yapacağız.
        [Theory, InlineData(1)]
        public void EditPOST_ValidModelState_UpdateMethodExecute(int productId)
        {   //_repository olduğu için mocklama yapacağız
            //Edit methodu içerisindeki Update() methodu void olduğu için yani geriye birşey dönmediği için bizde mocklama sonrasında Returns anahtar kelimesiyle geriye birşey dönmeyeceğiz.
            //mock içerisinde product verebilmek için önce yukarıda bir product tanımlayalım.
            var product = products.First(x => x.Id==productId);
            _mockRepo.Setup(repo => repo.Update(product));
            _controller.Edit(productId, product);
            //edit methodu çalıştığında update in çalışıp çalışmadığını kontrol edicem.
            _mockRepo.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Once);
            
        }







    }
}
