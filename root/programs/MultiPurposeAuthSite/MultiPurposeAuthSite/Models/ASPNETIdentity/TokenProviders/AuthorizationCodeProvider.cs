﻿//**********************************************************************************
//* Copyright (C) 2017 Hitachi Solutions,Ltd.
//**********************************************************************************

#region Apache License
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

//**********************************************************************************
//* クラス名        ：AuthorizationCodeProvider
//* クラス日本語名  ：AuthorizationCodeProvider（ライブラリ）
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
using MultiPurposeAuthSite.Models.ASPNETIdentity.OAuth2Extension;

using System;
using System.IO;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Threading.Tasks;

using System.Web;

using Microsoft.Owin;
using Microsoft.Owin.Security.Infrastructure;

using Dapper;
using Newtonsoft.Json;

namespace MultiPurposeAuthSite.Models.ASPNETIdentity.TokenProviders
{
    /// <summary>
    /// AuthorizationCodeのProvider
    /// TokenにSerializeTicket一時保存する。
    /// （Cluster対応する場合、ストアを用意する必要がある）
    /// </summary>
    /// <see cref="https://msdn.microsoft.com/ja-jp/library/microsoft.owin.security.infrastructure.authenticationtokenprovider.aspx"/>
    /// <seealso cref="https://msdn.microsoft.com/ja-jp/library/dn385573.aspx"/>
    public class AuthorizationCodeProvider : IAuthenticationTokenProvider
    {
        /// <summary>シングルトン</summary>
        private static AuthorizationCodeProvider _AuthorizationCodeProvider = new AuthorizationCodeProvider();
        
        /// <summary>
        /// _authenticationCodes
        /// ConcurrentDictionaryは、.NET 4.0の新しいスレッドセーフなHashtable
        /// </summary>
        private readonly ConcurrentDictionary<string, string>
                    _authenticationCodes = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        /// <summary>GetInstance</summary>
        /// <returns>AuthorizationCodeProvider</returns>
        public static AuthorizationCodeProvider GetInstance()
        {
            return AuthorizationCodeProvider._AuthorizationCodeProvider;
        }

        #region Create

        /// <summary>Create</summary>
        /// <param name="context">AuthenticationTokenCreateContext</param>
        public void Create(AuthenticationTokenCreateContext context)
        {
            this.CreateAuthenticationCode(context);
        }

        /// <summary>CreateAsync</summary>
        /// <param name="context">AuthenticationTokenCreateContext</param>
        /// <returns>Task</returns>
        public Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            return Task.Factory.StartNew(() => this.CreateAuthenticationCode(context));
        }

        /// <summary>CreateAuthenticationCode</summary>
        /// <param name="context">AuthenticationTokenCreateContext</param>
        private void CreateAuthenticationCode(AuthenticationTokenCreateContext context)
        {
            context.SetToken(Guid.NewGuid().ToString("n") + Guid.NewGuid().ToString("n"));

            Dictionary<string, string> temp = new Dictionary<string, string>();
            NameValueCollection queryString = HttpUtility.ParseQueryString(context.Request.QueryString.Value);

            temp.Add("ticket", context.SerializeTicket());
            temp.Add("code_challenge", queryString["code_challenge"]);
            temp.Add("code_challenge_method", queryString["code_challenge_method"]);

            string value = JsonConvert.SerializeObject(temp);

            switch (ASPNETIdentityConfig.UserStoreType)
            {
                case EnumUserStoreType.Memory:
                    _authenticationCodes[context.Token] = value;
                    break;

                case EnumUserStoreType.SqlServer:
                case EnumUserStoreType.ODPManagedDriver:
                case EnumUserStoreType.PostgreSQL: // DMBMS

                    using (IDbConnection cnn = DataAccess.CreateConnection())
                    {
                        cnn.Open();

                        switch (ASPNETIdentityConfig.UserStoreType)
                        {
                            case EnumUserStoreType.SqlServer:

                                cnn.Execute(
                                    "INSERT INTO [AuthenticationCodeDictionary] ([Key], [Value], [CreatedDate]) VALUES (@Key, @Value, @CreatedDate)",
                                    new { Key = context.Token, Value = value, CreatedDate = DateTime.Now });

                                break;

                            case EnumUserStoreType.ODPManagedDriver:

                                cnn.Execute(
                                    "INSERT INTO \"AuthenticationCodeDictionary\" (\"Key\", \"Value\", \"CreatedDate\") VALUES (:Key, :Value, :CreatedDate)",
                                    new { Key = context.Token, Value = value, CreatedDate = DateTime.Now });

                                break;

                            case EnumUserStoreType.PostgreSQL:

                                cnn.Execute(
                                    "INSERT INTO \"authenticationcodedictionary\" (\"key\", \"value\", \"createddate\") VALUES (@Key, @Value, @CreatedDate)",
                                    new { Key = context.Token, Value = value, CreatedDate = DateTime.Now });

                                break;
                        }
                    }

                    break;
            }
        }

        #endregion

        #region Receive

        /// <summary>Receive</summary>
        /// <param name="context">AuthenticationTokenReceiveContext</param>
        public void Receive(AuthenticationTokenReceiveContext context)
        {
            this.ReceiveAuthenticationCode(context);
        }

        /// <summary>ReceiveAsync</summary>
        /// <param name="context">AuthenticationTokenReceiveContext</param>
        /// <returns>Task</returns>
        public Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {
            return Task.Factory.StartNew(() => this.ReceiveAuthenticationCode(context));
        }

        /// <summary>ReceiveAuthenticationCode</summary>
        /// <param name="context">AuthenticationTokenReceiveContext</param>
        private void ReceiveAuthenticationCode(AuthenticationTokenReceiveContext context)
        {
            context.Request.Body.Position = 0;

            string code_verifier = null;
            string body = new StreamReader(context.Request.Body).ReadToEnd();

            if (body.IndexOf("code_verifier=") != -1)
            {
                string[] forms = body.Split('&');
                foreach (string form in forms)
                {
                    if (form.StartsWith("code_verifier="))
                    {
                        code_verifier = form.Split('=')[1];
                    }
                }
            }

            string value = "";
            switch (ASPNETIdentityConfig.UserStoreType)
            {
                case EnumUserStoreType.Memory:
                    if (_authenticationCodes.TryRemove(context.Token, out value))
                    {
                        context.DeserializeTicket(this.Verifier(value, code_verifier));
                    }
                    break;

                case EnumUserStoreType.SqlServer:
                case EnumUserStoreType.ODPManagedDriver:
                case EnumUserStoreType.PostgreSQL: // DMBMS

                    using (IDbConnection cnn = DataAccess.CreateConnection())
                    {
                        cnn.Open();

                        switch (ASPNETIdentityConfig.UserStoreType)
                        {
                            case EnumUserStoreType.SqlServer:

                                value = cnn.ExecuteScalar<string>(
                                  "SELECT [Value] FROM [AuthenticationCodeDictionary] WHERE [Key] = @Key", new { Key = context.Token });

                                context.DeserializeTicket(this.Verifier(value, code_verifier));

                                cnn.Execute(
                                    "DELETE FROM [AuthenticationCodeDictionary] WHERE [Key] = @Key", new { Key = context.Token });

                                break;

                            case EnumUserStoreType.ODPManagedDriver:

                                value = cnn.ExecuteScalar<string>(
                                    "SELECT \"Value\" FROM \"AuthenticationCodeDictionary\" WHERE \"Key\" = :Key", new { Key = context.Token });

                                context.DeserializeTicket(this.Verifier(value, code_verifier));

                                cnn.Execute(
                                    "DELETE FROM \"AuthenticationCodeDictionary\" WHERE \"Key\" = :Key", new { Key = context.Token });

                                break;

                            case EnumUserStoreType.PostgreSQL:

                                value = cnn.ExecuteScalar<string>(
                                    "SELECT \"value\" FROM \"authenticationcodedictionary\" WHERE \"key\" = @Key", new { Key = context.Token });

                                context.DeserializeTicket(this.Verifier(value, code_verifier));

                                cnn.Execute(
                                    "DELETE FROM \"authenticationcodedictionary\" WHERE \"key\" = @Key", new { Key = context.Token });

                                break;
                        }
                    }

                    break;
            }
        }

        /// <summary>Verifier</summary>
        /// <param name="value">string</param>
        /// <param name="code_verifier">string</param>
        /// <returns>ticket</returns>
        private string Verifier(string value, string code_verifier)
        {
            Dictionary<string, string> temp = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);

            bool isPKCE = (code_verifier != null);
            
            if (!isPKCE)
            {
                // 通常のアクセストークン・リクエスト
                if (string.IsNullOrEmpty(temp["code_challenge"]))
                {
                    // Authorization Codeのcode
                    return temp["ticket"];
                }
                else
                {
                    // OAuth PKCEのcode（要 code_verifier）
                    return "";
                }
            }
            else
            {
                // OAuth PKCEのアクセストークン・リクエスト
                if (!string.IsNullOrEmpty(temp["code_challenge"]) && !string.IsNullOrEmpty(code_verifier))
                {
                    if (temp["code_challenge_method"].ToLower() == "plain")
                    {
                        // plain
                        if (temp["code_challenge"] == code_verifier)
                        {
                            // 検証成功
                            return temp["ticket"];
                        }
                        else
                        {
                            // 検証失敗
                        }
                    }
                    else if (temp["code_challenge_method"].ToLower() == "s256")
                    {
                        // s256
                        if (temp["code_challenge"] == OAuth2Helper.PKCE_S256_CodeChallengeMethod(code_verifier))
                        {
                            // 検証成功
                            return temp["ticket"];
                        }
                        else
                        {
                            // 検証失敗
                        }
                    }
                    else
                    {
                        // パラメタ不正
                    }
                }
                else
                {
                    // パラメタ不正
                }

                return null;
            }
        }

        #endregion
    }
}