using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RTDWebAPI.Interface;
using RTDWebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml;

namespace RTDWebAPI.Controllers
{
    public class AccountController : Controller
    {
        IFunctionService functionService;
        IBaseDataService baseDataService;
        IConfiguration configuration;

        public AccountController(IConfiguration _configuration, IFunctionService _functionService, IBaseDataService _baseDataService)
        {
            baseDataService = _baseDataService;
            functionService = _functionService;
            configuration = _configuration;
        }
        //Sample Users Data, it can be fetched with the use of any ORM
        public List<UserModel> users = null;
        

        public IActionResult Login(string ReturnUrl = "/")
        {
            LoginModel objLoginModel = new LoginModel();
            objLoginModel.ReturnUrl = ReturnUrl;
            return View(objLoginModel);
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel objLoginModel)
        {
            if (ModelState.IsValid)
            {
                // var user = users.Where(x => x.Username == objLoginModel.UserName && x.Password == objLoginModel.Password).FirstOrDefault();
                if (objLoginModel==null)
                {
                    //Add logic here to display some message to user
                    ViewBag.Message = "Invalid Credential";
                    return View(objLoginModel);
                }
                else
                {
                    
                    string webserurl = configuration["WebService:url"];
                    Console.WriteLine("hahaha:----" + webserurl);
                    var binding = new BasicHttpBinding();
                    //根據WebService 的URL 構建終端點對象，參數是提供的WebService地址
                    var endpoint = new EndpointAddress(String.Format(@" {0} ", webserurl));
                    //創建調用接口的工廠，注意這裡泛型只能傳入接口泛型接口裡面的參數是WebService裡面定義的類名+Soap 
                    //var factory = new ChannelFactory<WebServiceSoap>(binding, endpoint);
                    //從工廠獲取具體的調用實例
                    //var callClient = factory.CreateChannel();
                    //調用具體的方法，這裡是HelloWorldAsync 方法


                    //調用TestMethod方法，並傳遞參數
                    //_TPQuery_CheckPROMISLoginRequestBody body = new _TPQuery_CheckPROMISLoginRequestBody(objLoginModel.UserName, objLoginModel.Password);
                    //Task<_TPQuery_CheckPROMISLoginResponse> testResponsePara = callClient._TPQuery_CheckPROMISLoginAsync(new _TPQuery_CheckPROMISLoginRequest(body));
                    //獲取
                    //string result3 = testResponsePara.Result.Body._TPQuery_CheckPROMISLoginResult;
                    //<?xml version="1.0" encoding="utf - 8"?><Beans><Status Value="FAILURE" /><ErrMsg Value="SECURITY. % UAF - W - LOGFAIL, user authorization failure, privileges removed." /></Beans>';
                    //string test = "<body><head>test header</head></body>";
                    XmlDocument xmlDoc = new XmlDocument();
                    //xmlDoc.LoadXml(result3);
                    XmlNode xn = xmlDoc.SelectSingleNode("Beans");


                    XmlNodeList xnlA = xn.ChildNodes;
                    String member_valodation = "";
                    String member_validation_message = "";
                    foreach (XmlNode xnA in xnlA)
                    {
                        Console.WriteLine(xnA.Name);
                        if ((xnA.Name) == "Status")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            if ((xeB.GetAttribute("Value")) == "SUCCESS")
                            {
                                member_valodation = "OK";
                            }
                            else
                            {
                                member_valodation = "NG";
                            }

                        }
                        if ((xnA.Name) == "ErrMsg")
                        {
                            XmlElement xeB = (XmlElement)xnA;
                            member_validation_message = xeB.GetAttribute("Value");
                        }

                        Console.WriteLine(member_valodation);
                    }
                    if (member_valodation=="OK")
                    {
                        //A claim is a statement about a subject by an issuer and
                        //represent attributes of the subject that are useful in the context of authentication and authorization operations.
                        if (objLoginModel.UserName=="admin")
                        {
                            objLoginModel.Role = "Admin";
                        }
                        else
                        {
                            objLoginModel.Role = "User";
                        }
                        var claims = new List<Claim>() {
                        //new Claim(ClaimTypes.NameIdentifier,Convert.ToString(user.UserId)),
                            new Claim("user_name",objLoginModel.UserName),
                            new Claim(ClaimTypes.Role,objLoginModel.Role),
                        //new Claim("FavoriteDrink","Tea")
                        };
                        //Initialize a new instance of the ClaimsIdentity with the claims and authentication scheme
                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        //Initialize a new instance of the ClaimsPrincipal with ClaimsIdentity
                        var principal = new ClaimsPrincipal(identity);
                        //SignInAsync is a Extension method for Sign in a principal for the specified scheme.
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                            principal, new AuthenticationProperties() { IsPersistent = objLoginModel.RememberLogin });

                        return LocalRedirect(objLoginModel.ReturnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("UserName", "Username Error");
                        ModelState.AddModelError("Password", "Password error");
                        return View(objLoginModel);
                    }
                    
                }
            }
            return View(objLoginModel);
        }

        public async Task<IActionResult> LogOut() {
            //SignOutAsync is Extension method for SignOut
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //Redirect to home page
            return LocalRedirect("/");
        }
    }
}
