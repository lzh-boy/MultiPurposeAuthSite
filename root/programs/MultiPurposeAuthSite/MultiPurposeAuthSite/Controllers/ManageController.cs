﻿//**********************************************************************************
//* テンプレート
//**********************************************************************************

// 以下のLicenseに従い、このProjectをTemplateとして使用可能です。Release時にCopyright表示してSublicenseして下さい。
// https://github.com/OpenTouryoProject/MultiPurposeAuthSite/blob/master/license/LicenseForTemplates.txt

//**********************************************************************************
//* クラス名        ：ManageController
//* クラス日本語名  ：ManageのController
//*
//* 作成日時        ：－
//* 作成者          ：－
//* 更新履歴        ：－
//*
//*  日時        更新者            内容
//*  ----------  ----------------  -------------------------------------------------
//*  2017/04/24  西野 大介         新規
//**********************************************************************************

using MultiPurposeAuthSite.Models.Util;
using MultiPurposeAuthSite.Models.ViewModels;
using MultiPurposeAuthSite.Models.ASPNETIdentity;
using MultiPurposeAuthSite.Models.ASPNETIdentity.Manager;
using MultiPurposeAuthSite.Models.ASPNETIdentity.Entity;
using MultiPurposeAuthSite.Models.ASPNETIdentity.ExternalLoginHelper;
using MultiPurposeAuthSite.Models.ASPNETIdentity.NotificationProvider;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;

using System;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;

using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Touryo.Infrastructure.Public.Util;
using Touryo.Infrastructure.Business.Presentation;

/// <summary>MultiPurposeAuthSite.Controllers</summary>
namespace MultiPurposeAuthSite.Controllers
{
    /// <summary>ManageController</summary>
    [Authorize]
    public class ManageController : MyBaseMVController
    {
        /// <summary>列挙型</summary>
        public enum EnumManageMessageId
        {
            /// <summary>SetPasswordSuccess</summary>
            SetPasswordSuccess,
            /// <summary>ChangePasswordSuccess</summary>
            ChangePasswordSuccess,
            /// <summary>SetTwoFactorSuccess</summary>
            SetTwoFactorSuccess,
            /// <summary>AddEmailSuccess</summary>
            AddEmailSuccess,
            /// <summary>RemoveEmailSuccess</summary>
            RemoveEmailSuccess,
            /// <summary>AddPhoneSuccess</summary>
            AddPhoneSuccess,
            /// <summary>RemovePhoneSuccess</summary>
            RemovePhoneSuccess,
            /// <summary>AddPaymentInformationSuccess</summary>
            AddPaymentInformationSuccess,
            /// <summary>RemovePaymentInformationSuccess</summary>
            RemovePaymentInformationSuccess,
            /// <summary>RemoveLoginSuccess</summary>
            RemoveLoginSuccess,
            /// <summary>AccountConflictInSocialLogin</summary>
            AccountConflictInSocialLogin,
            /// <summary>Error</summary>
            Error
        }
        
        #region constructor

        /// <summary>constructor</summary>
        public ManageController() { }

        #endregion

        #region property (GetOwinContext)

        /// <summary>ApplicationUserManager</summary>
        public ApplicationUserManager UserManager
        {
            get
            {
                return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        /// <summary>ApplicationRoleManager</summary>
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return HttpContext.GetOwinContext().GetUserManager<ApplicationRoleManager>();
            }
        }

        /// <summary>ApplicationSignInManager</summary>
        public ApplicationSignInManager SignInManager
        {
            get
            {
                return HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
        }

        /// <summary>AuthenticationManager</summary>
        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        #endregion

        #region Action Method

        #region 管理画面（初期表示）

        /// <summary>
        /// 管理画面（初期表示）
        /// GET: /Manage/Index
        /// </summary>
        /// <param name="message">ManageMessageId</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpGet]
        public async Task<ActionResult> Index(EnumManageMessageId? message)
        {
            // 色々な結果メッセージの設定
            ViewBag.StatusMessage =
                message == EnumManageMessageId.SetPasswordSuccess ? Resources.ManageController.SetPasswordSuccess
                : message == EnumManageMessageId.ChangePasswordSuccess ? Resources.ManageController.ChangePasswordSuccess
                : message == EnumManageMessageId.SetTwoFactorSuccess ? Resources.ManageController.SetTwoFactorSuccess
                : message == EnumManageMessageId.AddEmailSuccess ? Resources.ManageController.AddEmailSuccess
                : message == EnumManageMessageId.RemoveEmailSuccess ? Resources.ManageController.RemoveEmailSuccess
                : message == EnumManageMessageId.AddPhoneSuccess ? Resources.ManageController.AddPhoneSuccess
                : message == EnumManageMessageId.RemovePhoneSuccess ? Resources.ManageController.RemovePhoneSuccess
                : message == EnumManageMessageId.AddPaymentInformationSuccess ? Resources.ManageController.AddPaymentInformationSuccess
                : message == EnumManageMessageId.RemovePaymentInformationSuccess ? Resources.ManageController.RemovePaymentInformationSuccess
                : message == EnumManageMessageId.RemoveLoginSuccess ? Resources.ManageController.RemoveLoginSuccess
                : message == EnumManageMessageId.AccountConflictInSocialLogin ? Resources.ManageController.AccountConflictInSocialLogin
                : message == EnumManageMessageId.Error ? Resources.ManageController.Error
                : "";
            
            // ユーザIDの取得
            //var userId = User.Identity.GetUserId();
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            // モデルの生成
            ManageIndexViewModel model = new ManageIndexViewModel
            {
                // パスワード
                HasPassword = await UserManager.HasPasswordAsync(user.Id), //HasPassword(),
                // ログイン
                Logins = user.Logins, //await UserManager.GetLoginsAsync(user.Id),
                // E-mail
                Email = user.Email, //await UserManager.GetEmailAsync(user.Id),
                // 電話番号
                PhoneNumber = user.PhoneNumber, //await UserManager.GetPhoneNumberAsync(user.Id),
                // 2FA
                TwoFactor = user.TwoFactorEnabled, //await UserManager.GetTwoFactorEnabledAsync(user.Id),
                // 支払元情報
                HasPaymentInformation = !string.IsNullOrEmpty(user.PaymentInformation)
            };

            // 管理画面の表示
            return View(model);
        }

        #endregion
        
        #region Password

        #region Create

        /// <summary>
        /// パスワード設定画面（初期表示）
        /// GET: /Manage/SetPassword
        /// </summary>
        /// <returns>ActionResult</returns>
        [HttpGet]
        public ActionResult SetPassword()
        {
            return View();
        }

        /// <summary>
        /// パスワード設定画面（パスワード設定）
        /// POST: /Manage/SetPassword
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(ManageSetPasswordViewModel model)
        {
            // SetPasswordViewModelの検証
            if (ModelState.IsValid)
            {
                // SetPasswordViewModelの検証に成功

                // パスワード設定
                IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

                // 結果の確認
                if (result.Succeeded)
                {
                    // 成功

                    // 再ログイン
                    await this.ReSignInAsync();

                    // Index - SetPasswordSuccess
                    return RedirectToAction("Index", new { Message = EnumManageMessageId.SetPasswordSuccess });
                }
                else
                {
                    // 失敗
                    AddErrors(result);
                }
            }
            else
            {
                // SetPasswordViewModelの検証に失敗
            }

            // If we got this far, something failed, redisplay form
            // ここに到達した場合は何らかの問題が発生しているので、フォームを再表示します。

            // 再表示
            return View(model);
        }

        #endregion

        #region Update

        /// <summary>
        /// パスワード変更画面（初期表示）
        /// GET: /Manage/ChangePassword
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// パスワード変更画面（パスワード変更）
        /// POST: /Manage/ChangePassword
        /// </summary>
        /// <param name="model">ChangePasswordViewModel</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ManageChangePasswordViewModel model)
        {
            // ManageChangePasswordViewModelの検証
            if (ModelState.IsValid)
            {
                // ManageChangePasswordViewModelの検証に成功

                // パスワード変更
                IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);

                // パスワードの変更結果の確認
                if (result.Succeeded)
                {
                    // 成功

                    // 再ログイン
                    await this.ReSignInAsync();

                    // Index - ChangePasswordSuccess
                    return RedirectToAction("Index", new { Message = EnumManageMessageId.ChangePasswordSuccess });
                }
                else
                {
                    // 失敗
                    AddErrors(result);
                }
            }
            else
            {
                // ManageChangePasswordViewModelの検証に失敗
            }

            // If we got this far, something failed, redisplay form
            // ここに到達した場合は何らかの問題が発生しているので、フォームを再表示します。

            // 再表示
            return View(model);

        }

        #endregion

        #endregion

        #region E-mail

        #region Create

        /// <summary>
        /// E-mailの追加画面（初期表示）
        /// GET: /Manage/AddEmail
        /// </summary>
        /// <returns>ActionResult</returns>
        [HttpGet]
        public ActionResult AddEmail()
        {
            return View();
        }

        /// <summary>
        /// E-mailの追加画面（E-mailの追加）
        /// POST: /Manage/AddEmail
        /// </summary>
        /// <param name="model">ManageAddEmailViewModel</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEmail(ManageAddEmailViewModel model)
        {
            // RegisterViewModelの検証
            if (ModelState.IsValid)
            {
                // RegisterViewModelの検証に成功
                // めんどうなのでSessionStoreで。
                string code = GetPassword.Base64UrlSecret(16);
                Session["Code"] = code;
                Session["Email"] = model.Email; // 更新後のメアド

                this.SendConfirmEmail(User.Identity.GetUserId(), model.Email, code);

                // 再表示
                return View("VerifyEmailAddress");
            }
            else
            {
                // RegisterViewModelの検証に失敗
            }

            // 再表示
            return View(model);
        }

        #endregion

        #region メアド検証

        /// <summary>
        /// メアド検証画面（メールからのリンクで結果表示）
        /// GET: /Manage/EmailConfirmation
        /// </summary>
        /// <param name="userId">string</param>
        /// <param name="code">string</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> EmailConfirmation(string userId, string code)
        {
            // 入力の検証1
            if (userId == null || code == null)
            {
                // エラー画面
                return View("Error");
            }
            else
            {
                // 入力の検証2
                //IdentityResult result = await UserManager.ConfirmEmailAsync(userId, code);

                // 自前の検証
                if(User.Identity.GetUserId() == userId)
                {
                    if (code == (string)Session["Code"])
                    {
                        // 更新後のメアド
                        IdentityResult result = await UserManager.SetEmailAsync(User.Identity.GetUserId(), (string)Session["Email"]);
                        
                        // 結果の確認
                        if (result.Succeeded)
                        {
                            // メアド検証の成功

                            // 再ログイン
                            if (await this.ReSignInAsync())
                            {
                                // 再ログインに成功
                                return RedirectToAction("Index", new { Message = EnumManageMessageId.AddEmailSuccess });
                            }
                            else
                            {
                                // 再ログインに失敗
                            }
                        }
                        else
                        {
                            // E-mail削除の失敗
                        }
                    }
                }

                // エラー画面
                return View("Error");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// E-mailの削除
        /// POST: /Manage/RemoveEmail
        /// </summary>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveEmail()
        {
            // null クリア
            IdentityResult result = await UserManager.SetEmailAsync(User.Identity.GetUserId(), "");

            // 結果の確認
            if (result.Succeeded)
            {
                // E-mail削除の成功

                // 再ログイン
                if (await this.ReSignInAsync())
                {
                    // 再ログインに成功
                    return RedirectToAction("Index", new { Message = EnumManageMessageId.RemoveEmailSuccess });
                }
                else
                {
                    // 再ログインに失敗
                }
            }
            else
            {
                // E-mail削除の失敗
            }

            // Index - Error
            return RedirectToAction("Index", new { Message = EnumManageMessageId.Error });
        }

        #endregion

        #endregion

        #region Phone Number

        #region Createプロセス

        #region Create

        /// <summary>
        /// 電話番号の追加画面（初期表示）
        /// GET: /Manage/AddPhoneNumber
        /// </summary>
        /// <returns>ActionResult</returns>
        [HttpGet]
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        /// <summary>
        /// 電話番号の追加画面（電話番号の追加）
        /// POST: /Manage/AddPhoneNumber
        /// </summary>
        /// <param name="model">AddPhoneNumberViewModel</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(ManageAddPhoneNumberViewModel model)
        {
            // RegisterViewModelの検証
            if (ModelState.IsValid)
            {
                // RegisterViewModelの検証に成功

                // 検証コード生成
                string code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);

                // メッセージをSMSで送信する。
                if (UserManager.SmsService != null)
                {
                    IdentityMessage message = new IdentityMessage
                    {
                        Destination = model.Number,
                        Body = Resources.ManageController.CodeForAddPhoneNumber + code
                    };
                    await UserManager.SmsService.SendAsync(message);
                }

                // 電話番号の検証画面に進む
                return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
            }
            else
            {
                // RegisterViewModelの検証に失敗

                // 再表示
                return View(model);
            }
        }

        #endregion

        #region Verify

        /// <summary>
        /// 追加電話番号の検証画面（初期表示）
        /// GET: /Manage/VerifyPhoneNumber
        /// </summary>
        /// <param name="phoneNumber">追加電話番号</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpGet]
        public ActionResult VerifyPhoneNumber(string phoneNumber)
        {   
            return phoneNumber == null ?
                View("Error") : 
                View("VerifyPhoneNumber", new ManageVerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        /// <summary>
        /// 追加電話番号の検証画面（検証コードの検証）
        /// POST: /Manage/VerifyPhoneNumber
        /// </summary>
        /// <param name="model">VerifyPhoneNumberViewModel</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(ManageVerifyPhoneNumberViewModel model)
        {
            // ManageVerifyPhoneNumberViewModelの検証
            if (ModelState.IsValid)
            {
                // ManageVerifyPhoneNumberViewModelの検証に成功

                // 電話番号の検証（電話番号の登録の際に、SMSで送信した検証コードを検証）
                IdentityResult result = await UserManager.ChangePhoneNumberAsync(
                    User.Identity.GetUserId(), model.PhoneNumber, model.Code);

                // 電話番号の検証結果の確認
                if (result.Succeeded)
                {
                    // 成功

                    // 再ログイン
                    await this.ReSignInAsync();

                    // Index - AddPhoneSuccess
                    return RedirectToAction("Index", new { Message = EnumManageMessageId.AddPhoneSuccess });
                }
                else
                {
                    // 失敗
                    ModelState.AddModelError("", Resources.ManageController.FailedVerifyPhoneNumber);
                }
            }
            else
            {
                // ManageVerifyPhoneNumberViewModelの検証に失敗
            }

            // If we got this far, something failed, redisplay form
            // ここに到達した場合は何らかの問題が発生しているので、フォームを再表示します。

            // 再表示
            return View(model);
        }

        #endregion

        #endregion

        #region Delete

        /// <summary>
        /// 電話番号の削除
        /// POST: /Manage/RemovePhoneNumber
        /// </summary>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            // null クリア
            IdentityResult result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), "");

            // 結果の確認
            if (result.Succeeded)
            {
                // 電話番号削除の成功

                // 再ログイン
                if (await this.ReSignInAsync())
                {
                    // 再ログインに成功
                    return RedirectToAction("Index", new { Message = EnumManageMessageId.RemovePhoneSuccess });
                }
                else
                {
                    // 再ログインに失敗
                }
            }
            else
            {
                // 電話番号削除の失敗
            }

            // Index - Error
            return RedirectToAction("Index", new { Message = EnumManageMessageId.Error });
        }

        #endregion

        #endregion

        #region 2FAの有効化・無効化

        /// <summary>
        /// 2FAの有効化
        /// POST: /Manage/EnableTwoFactorAuthentication
        /// </summary>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            // 2FAの有効化
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);

            // 再ログイン
            await this.ReSignInAsync();

            return RedirectToAction("Index", "Manage");
        }

        /// <summary>
        /// 2FAの無効化
        /// POST: /Manage/DisableTwoFactorAuthentication
        /// </summary>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            // 2FAの無効化
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);

            // 再ログイン
            await this.ReSignInAsync();

            return RedirectToAction("Index", "Manage");
        }

        #endregion

        #region (External) Logins

        /// <summary>
        /// 外部ログイン管理画面（初期表示）
        /// GET: /Manage/ManageLogins
        /// </summary>
        /// <param name="message">ManageMessageId</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpGet]
        public async Task<ActionResult> ManageLogins(EnumManageMessageId? message)
        {
            // 色々な結果メッセージの設定
            ViewBag.StatusMessage =
               message == EnumManageMessageId.Error ? Resources.ManageController.Error
               : message == EnumManageMessageId.RemovePhoneSuccess ? Resources.ManageController.RemovePhoneSuccess
               : message == EnumManageMessageId.AccountConflictInSocialLogin ? Resources.ManageController.AccountConflictInSocialLogin
               : "";

            // 認証されたユーザを取得
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            // 現在の認証ユーザが外部ログイン済みの外部ログイン情報を取得
            IList<UserLoginInfo> userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());

            // 現在の認証ユーザが未ログインの外部ログイン情報を取得
            List<AuthenticationDescription> otherLogins = new List<AuthenticationDescription>();

            #region Collect otherLogins
            //// auth : AuthenticationDescription
            //// ul   : UserLoginInfo
            //otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().
            //    Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();

            // 以下と等価（どっちが楽？）

            IEnumerable<AuthenticationDescription> allExternalLogins = AuthenticationManager.GetExternalAuthenticationTypes();
            foreach (AuthenticationDescription auth in allExternalLogins)
            {
                bool flg = true;
                foreach (UserLoginInfo ul in userLogins)
                {
                    if (auth.AuthenticationType == ul.LoginProvider)
                    {
                        flg = false;
                    }
                }

                // userLoginsに存在しないものだけ追加
                if (flg) otherLogins.Add(auth);
            }
            #endregion

            // 削除ボタンを表示するかしないか。
            // 通常ログインがあるか、外部ログイン・カウントが１以上ある場合に表示する。
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;

            // 表示
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        /// <summary>
        /// ログイン削除
        /// POST: /Manage/RemoveLogin
        /// </summary>
        /// <param name="loginProvider">loginProvider</param>
        /// <param name="providerKey">providerKey</param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            // メッセージ列挙型
            EnumManageMessageId? message;

            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            IdentityResult result = null;

            // クレームを削除
            // Memory Provider使用の時
            // 「コレクションが変更されました。列挙操作は実行されない可能性があります。」
            // 問題で若干冗長なコードに。
            List<Claim> lc = new List<Claim>();
            foreach (Claim c in user.Claims)
            {
                if(c.Issuer == loginProvider)
                {
                    lc.Add(c);
                }
            }
            foreach (Claim c in lc)
            {
                result = await UserManager.RemoveClaimAsync(user.Id, c);
            }

            // ログインを削除
            result = await UserManager.RemoveLoginAsync(
                User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));

            // 結果の確認
            if (result.Succeeded)
            {
                // ログイン削除の成功

                // 再ログイン
                if (await this.ReSignInAsync())
                {
                    // 再ログインに成功
                    message = EnumManageMessageId.RemoveLoginSuccess;
                }
                else
                {
                    // 再ログインに失敗
                    message = EnumManageMessageId.Error;
                }
            }
            else
            {
                // ログイン削除の失敗
                message = EnumManageMessageId.Error;
            }

            // ログイン管理画面（ログイン削除結果
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        /// <summary>
        /// 外部ログイン（リダイレクト）の開始
        /// POST: /Manage/ExternalLogin
        /// </summary>
        /// <param name="provider">string</param>
        /// <returns>ActionResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider)
        {
            // Request a redirect to the external login provider
            //  to link a login for the current user.
            // 現在のユーザーのログインをリンクするために
            // 外部ログイン プロバイダーへのリダイレクトを要求します。
            return new ExternalLoginStarter(
                provider,
                Url.Action("ExternalLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        /// <summary>
        /// 外部LoginのCallback（ExternalLoginCallback）
        /// Redirect後、外部Login providerに着信し、そこで、
        /// URL fragmentを切捨てCookieに認証Claim情報を設定、
        /// その後、ココにRedirectされ、認証Claim情報を使用してSign-Inする。
        /// （外部Login providerからRedirectで戻る先のURLのAction method）
        /// GET: /Manage/ExternalLoginCallback
        /// </summary>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpGet]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            // AccountControllerはサインアップかどうかを判定して処理する必要がある。
            // ManageControllerは判定不要だが、サインイン後なので、uidが一致する必要がある。

            // asp.net mvc - MVC 5 Owin Facebook Auth results in Null Reference Exception - Stack Overflow
            // http://stackoverflow.com/questions/19564479/mvc-5-owin-facebook-auth-results-in-null-reference-exception

            // ログイン プロバイダーが公開している認証済みユーザーに関する情報を受け取る。
            AuthenticateResult authenticateResult = await AuthenticationManager.AuthenticateAsync(DefaultAuthenticationTypes.ExternalCookie);
            // 外部ログイン・プロバイダからユーザに関する情報を取得する。
            ExternalLoginInfo externalLoginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();

            IdentityResult result = null;
            //SignInStatus signInStatus = SignInStatus.Failure;

            if (authenticateResult != null
                && authenticateResult.Identity != null
                && externalLoginInfo != null)
            {
                // ログイン情報を受け取れた場合、クレーム情報を分析
                ClaimsIdentity claims = authenticateResult.Identity;

                // ID情報とe-mail, name情報は必須
                Claim idClaim = claims.FindFirst(ClaimTypes.NameIdentifier);
                Claim emailClaim = claims.FindFirst(ClaimTypes.Email);
                Claim nameClaim = claims.FindFirst(ClaimTypes.Name);

                // 外部ログインで取得するクレームを標準化する。
                // ・・・
                // ・・・
                // ・・・

                if (idClaim != null)
                {
                    // UserLoginInfoの生成
                    UserLoginInfo login = new UserLoginInfo(idClaim.Issuer, idClaim.Value);

                    // クレーム情報（ID情報とe-mail, name情報）を抽出
                    string id = idClaim.Value;
                    string name = nameClaim.Value;
                    string email = "";

                    #region nameClaim対策 (今の所無し)
                    //・・・
                    #endregion

                    #region emailClaim対策 (Facebook)
                    if (emailClaim == null)
                    {
                        // emailClaimが取得できなかった場合、
                        if (externalLoginInfo.Login.LoginProvider == "Facebook")
                        {
                            var identity = AuthenticationManager.GetExternalIdentity(DefaultAuthenticationTypes.ExternalCookie);
                            var access_token = identity.FindFirstValue("FacebookAccessToken");
                            var fb = new Facebook.FacebookClient(access_token);
                            dynamic myInfo = fb.Get("/me?fields=email"); // specify the email field
                            email = myInfo.email; // Facebookでは、emailClaimを取得できない。
                        }
                    }
                    else
                    {
                        // emailClaimが取得できた場合、
                        email = emailClaim.Value;
                    }
                    #endregion

                    string uid = "";
                    if (ASPNETIdentityConfig.RequireUniqueEmail)
                    {
                        uid = email;
                    }
                    else
                    {
                        uid = name;
                    }

                    if (!string.IsNullOrEmpty(email)
                        && !string.IsNullOrEmpty(name))
                    {
                        // クレーム情報（e-mail, name情報）を取得できた。

                        // 既存の外部ログインを確認する。
                        ApplicationUser user = await UserManager.FindAsync(login);

                        if (user != null)
                        {
                            // 既存の外部ログインがある場合。

                            // ユーザーが既に外部ログインしている場合は、クレームをRemove, Addで更新し、
                            result = await UserManager.RemoveClaimAsync(user.Id, emailClaim); // del-ins
                            result = await UserManager.AddClaimAsync(user.Id, emailClaim);
                            result = await UserManager.RemoveClaimAsync(user.Id, nameClaim); // del-ins
                            result = await UserManager.AddClaimAsync(user.Id, nameClaim);

                            //// SignInAsyncより、ExternalSignInAsyncが適切。

                            ////// 通常のサインイン
                            ////await SignInManager.SignInAsync(

                            //// 既存の外部ログイン・プロバイダでサインイン
                            //signInStatus = await SignInManager.ExternalSignInAsync(
                            //                     loginInfo: externalLoginInfo,
                            //                     isPersistent: false); // 外部ログインの Cookie 永続化は常に false.

                            // ManageControllerではサインイン済みなので、何もしない。
                            return RedirectToAction("ManageLogins");
                        }
                        else
                        {
                            // 既存の外部ログインがない。

                            // ManageControllerではサインアップ・サインイン
                            // 済みなので、外部ログインの追加のみ行なう。

                            // サインアップ済みの可能性を探る
                            user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

                            // uid（e-mail, name情報）が一致している必要がある。
                            if (user.UserName == uid)
                            {
                                // uid（e-mail, name情報）が一致している。

                                // 外部ログイン（ = UserLoginInfo ）の追加
                                result = await UserManager.AddLoginAsync(user.Id, externalLoginInfo.Login);

                                // クレーム（emailClaim, nameClaim, etc.）の追加
                                if (result.Succeeded)
                                {
                                    result = await UserManager.AddClaimAsync(user.Id, emailClaim);
                                    result = await UserManager.AddClaimAsync(user.Id, nameClaim);
                                    // ・・・
                                    // ・・・
                                    // ・・・
                                }

                                // 上記の結果の確認
                                if (result.Succeeded)
                                {
                                    // 外部ログインの追加に成功した場合 → サインイン

                                    // SignInAsync、ExternalSignInAsync
                                    // 通常のサインイン（外部ログイン「追加」時はSignInAsyncを使用する）
                                    await SignInManager.SignInAsync(
                                        user,
                                        isPersistent: false,    // rememberMe は false 固定（外部ログインの場合）
                                        rememberBrowser: true); // rememberBrowser は true 固定

                                    //// この外部ログイン・プロバイダでサインイン
                                    //signInStatus = await SignInManager.ExternalSignInAsync(

                                    // リダイレクト
                                    return RedirectToAction("ManageLogins");
                                }
                                else
                                {
                                    // 外部ログインの追加に失敗した場合

                                    // 結果のエラー情報を追加
                                    AddErrors(result);
                                }
                            }
                            else
                            {
                                // uid（e-mail, name情報）が一致していない。
                                // 外部ログインのアカウントを間違えている。
                                return RedirectToAction("ManageLogins", new { Message = EnumManageMessageId.AccountConflictInSocialLogin });

                            } // else処理済
                        } // else処理済
                    } // クレーム情報（e-mail, name情報）を取得できなかった。
                } // クレーム情報（ID情報）を取得できなかった。
            } // ログイン情報を取得できなかった。

            // ログイン情報を受け取れなかった場合や、その他の問題が在った場合。
            return RedirectToAction("ManageLogins", new { Message = EnumManageMessageId.Error });
        }

        #endregion

        #region Payment Information

        #region Create

        /// <summary>
        /// 支払元情報の追加画面（初期表示）
        /// GET: /Manage/AddPaymentInformation
        /// </summary>
        /// <returns>ActionResult</returns>
        [HttpGet]
        public ActionResult AddPaymentInformation()
        {
            if (ASPNETIdentityConfig.EnableStripe)
            {
                ViewBag.PublishableKey = ASPNETIdentityConfig.Stripe_PK;
                return View("AddPaymentInformationStripe");
            }
            else if (ASPNETIdentityConfig.EnablePAYJP)
            {
                ViewBag.PublishableKey = ASPNETIdentityConfig.PAYJP_PK;
                return View("AddPaymentInformationPAYJP");
            }
            else
            {
                throw new NotSupportedException("Payment service is not enabled.");
            }
        }

        /// <summary>
        /// 支払元情報の追加画面（支払元情報設定）
        /// POST: /Manage/AddPaymentInformation
        /// </summary>
        /// <param name="model"></param>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPaymentInformation(ManageAddPaymentInformationViewModel model)
        {
            // ManageAddPaymentInformationViewModelの検証
            if (ModelState.IsValid)
            {
                // ManageAddPaymentInformationViewModelの検証に成功

                // ユーザの検索
                ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

                if (user != null)
                {
                    // ユーザを取得できた。

                    // TokenからClientIDに変換する。
                    JObject jobj = await WebAPIHelper.GetInstance().CreateaCustomerAsync(user.Email, model.PaymentInformation);
                    
                    // 支払元情報（ClientID）の設定
                    user.PaymentInformation = (string)jobj["id"];
                    // ユーザーの保存
                    IdentityResult result = await UserManager.UpdateAsync(user);

                    // 結果の確認
                    if (result.Succeeded)
                    {
                        // 成功

                        // 再ログイン
                        await this.ReSignInAsync();

                        // Index - SetPasswordSuccess
                        return RedirectToAction("Index", new { Message = EnumManageMessageId.AddPaymentInformationSuccess });
                    }
                    else
                    {
                        // 失敗
                        AddErrors(result);
                    }
                }
                else
                {
                    // ユーザを取得できなかった。
                }
            }
            else
            {
                // ManageAddPaymentInformationViewModelの検証に失敗
            }

            // If we got this far, something failed, redisplay form
            // ここに到達した場合は何らかの問題が発生しているので、フォームを再表示します。

            // 再表示
            if (ASPNETIdentityConfig.EnableStripe)
            {
                ViewBag.PublishableKey = ASPNETIdentityConfig.Stripe_PK;
                return View("AddPaymentInformationStripe");
            }
            else if (ASPNETIdentityConfig.EnablePAYJP)
            {
                ViewBag.PublishableKey = ASPNETIdentityConfig.PAYJP_PK;
                return View("AddPaymentInformationPAYJP");
            }
            else
            {
                throw new NotSupportedException("Payment service is not enabled.");
            }
        }

        #endregion

        #region Charge

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChargeByPaymentInformation()
        {
            // ユーザの検索
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            // 課金のテスト処理
            JObject jobj = await WebAPIHelper.GetInstance().ChargeToCustomers(user.PaymentInformation, "jpy", "1000");
            // 元の画面に戻る
            return RedirectToAction("Index");
        }

        #endregion

        #region Delete

        /// <summary>
        /// 支払元情報の削除
        /// POST: /Manage/RemovePaymentInformation
        /// </summary>
        /// <returns>ActionResultを非同期に返す</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePaymentInformation()
        {
            // ユーザの検索
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            // 支払元情報のクリア
            user.PaymentInformation = "";
            // ユーザーの保存
            IdentityResult result = await UserManager.UpdateAsync(user);

            // 結果の確認
            if (result.Succeeded)
            {
                // 支払元情報 削除の成功

                // 再ログイン
                if (await this.ReSignInAsync())
                {
                    // 再ログインに成功
                    return RedirectToAction("Index", new { Message = EnumManageMessageId.RemovePaymentInformationSuccess });
                }
                else
                {
                    // 再ログインに失敗
                }
            }
            else
            {
                // 支払元情報 削除の失敗
            }

            // Index - Error
            return RedirectToAction("Index", new { Message = EnumManageMessageId.Error });
        }

        #endregion

        #endregion

        #endregion

        #region Dispose

        /// <summary>Dispose</summary>
        /// <param name="disposing">bool</param>
        protected override void Dispose(bool disposing)
        {
            // メンバのdisposingを実装しているらしい。
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Helper

        #region 再度サインイン

        /// <summary>
        /// 再度サインインする。
        /// Cookie再設定 or SecurityStamp対応
        /// </summary>
        /// <returns>Task</returns>
        private async Task<bool> ReSignInAsync()
        {
            // 認証されたユーザを取得
            ApplicationUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            // ユーザの確認
            if (user != null)
            {
                // 認証されたユーザが無い
                // 再度サインイン
                await SignInManager.SignInAsync(
                        user,
                        isPersistent: false,        // アカウント記憶    // 既定値
                        rememberBrowser: true);     // ブラウザ記憶(2FA) // 既定値

                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Controller → View

        /// <summary>
        /// ModelStateDictionaryに
        /// IdentityResult.Errorsの情報を移送
        /// </summary>
        /// <param name="result">IdentityResult</param>
        private void AddErrors(IdentityResult result)
        {
            foreach (string error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        #endregion

        #region メアド検証、パスワード リセットのメール送信処理

        /// <summary>
        /// メアド検証、パスワード リセットで使用するメール送信処理。
        /// </summary>
        /// <param name="uid">string</param>
        /// <param name="email">string</param>
        /// <param name="code">string</param>
        private async void SendConfirmEmail(string uid, string email, string code)
        {
            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
            // アカウント確認とパスワード リセットを有効にする方法の詳細については、http://go.microsoft.com/fwlink/?LinkID=320771 を参照してください

            // Account Confirmation and Password Recovery with ASP.NET Identity (C#) | The ASP.NET Site
            // http://www.asp.net/identity/overview/features-api/account-confirmation-and-password-recovery-with-aspnet-identity

            string callbackUrl;
            
            // URLの生成
            callbackUrl = this.Url.Action(
                    "EmailConfirmation", "Manage",
                    new { userId = uid, code = code }, protocol: Request.Url.Scheme
                );

            // E-mailの送信
            //await UserManager.SendEmailAsync(
            //        user.Id,
            //        Resources.AccountController.SendEmail_emailconfirm,
            //        string.Format(Resources.AccountController.SendEmail_emailconfirm_msg, callbackUrl));

            EmailService ems = new EmailService();
            IdentityMessage idmsg = new IdentityMessage();

            idmsg.Subject = Resources.AccountController.SendEmail_emailconfirm;
            idmsg.Destination = email;
            idmsg.Body = string.Format(Resources.AccountController.SendEmail_emailconfirm_msg, callbackUrl);
            
            await ems.SendAsync(idmsg);
        }

        #endregion

        #endregion
    }
}