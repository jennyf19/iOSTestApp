﻿using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace iOSTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
#pragma warning disable AvoidAsyncVoid // async methods should return Task, but here this is imposed by Xamarin
        protected override async void OnAppearing()
#pragma warning restore AvoidAsyncVoid
        {
            // let's see if we have a user in the cache
            try
            {
                IEnumerable<IAccount> accounts = await App.PCA.GetAccountsAsync();
                IAccount firstAccount = accounts.FirstOrDefault();
                AuthenticationResult ar =
                    await App.PCA.AcquireTokenSilent(App.Scopes, firstAccount)
                    .ExecuteAsync();
                await RefreshUserDataAsync(ar.AccessToken);
                Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign out"; });
            }
            catch
            {
                // doesn't matter, we go in interactive more
                Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign in"; });
            }
        }
        async void OnSignInSignOut(object sender, EventArgs e)
        {
            try
            {
                if (btnSignInSignOut.Text == "Sign in")
                {
                    AuthenticationResult ar = await App.PCA.AcquireTokenInteractive(App.Scopes)
                        .ExecuteAsync();
                    await RefreshUserDataAsync(ar.AccessToken);
                    Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign out"; });
                }
                else
                {
                    IEnumerable<IAccount> accounts = await App.PCA.GetAccountsAsync();
                    IAccount firstAccount = accounts.FirstOrDefault();
                    await App.PCA.RemoveAsync(firstAccount);

                    slUser.IsVisible = false;
                    Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign in"; });
                }
            }
            catch (Exception ex)
            {
                lblDisplayName.Text = ex.Message;
            }
        }

        async void OnB2CSignInSignOut(object sender, EventArgs e)
        {
            IPublicClientApplication pca;
            var builder = PublicClientApplicationBuilder.Create(App.B2cClientId)
               .WithB2CAuthority(App.AuthoritySignInSignUp)
               .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
               .WithRedirectUri($"msal{App.B2cClientId}://auth");
            pca = builder.Build();
            try
            {
                if (btnB2CSignInSignOut.Text == "B2C Sign in")
                {
                    AuthenticationResult ar = await pca.AcquireTokenInteractive(App.B2cScopes)
                        .WithB2CAuthority(App.AuthoritySignInSignUp)
                        .WithPrompt(Prompt.NoPrompt)
                        .ExecuteAsync();
                    await CallB2CWebApi(ar.AccessToken);
                    Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign out"; });
                }
                else
                {
                    IEnumerable<IAccount> accounts = await pca.GetAccountsAsync();
                    while (accounts.Any())
                    {
                        await pca.RemoveAsync(accounts.FirstOrDefault());
                        accounts = await pca.GetAccountsAsync();
                    }

                    slUser.IsVisible = false;
                    Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "B2C Sign in"; });
                }
            }
            catch (Exception ex)
            {
                lblDisplayName.Text = ex.Message;
            }
        }

        private IAccount GetAccountByPolicy(IEnumerable<IAccount> accounts, string policy)
        {
            foreach (var account in accounts)
            {
                string userIdentifier = account.HomeAccountId.ObjectId.Split('.')[0];
                if (userIdentifier.EndsWith(policy.ToLower())) return account;
            }

            return null;
        }

        public async Task CallB2CWebApi(string token)
        {
            //get data from API
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, App.ApiEndpoint);
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.SendAsync(message);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject user = JObject.Parse(responseString);

                slUser.IsVisible = true;

                Device.BeginInvokeOnMainThread(() =>
                {
                    lblDisplayName.Text = user["name"].ToString();

                    // just in case
                    btnSignInSignOut.Text = "Sign out";
                });
            }
            else
            {
                await DisplayAlert("Something went wrong with the API call", responseString, "Dismiss");
            }
        }

        public async Task RefreshUserDataAsync(string token)
        {
            //get data from API
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            HttpResponseMessage response = await client.SendAsync(message);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject user = JObject.Parse(responseString);

                slUser.IsVisible = true;

                Device.BeginInvokeOnMainThread(() =>
                {

                    lblDisplayName.Text = user["displayName"].ToString();
                    lblGivenName.Text = user["givenName"].ToString();
                    lblId.Text = user["id"].ToString();
                    lblSurname.Text = user["surname"].ToString();
                    lblUserPrincipalName.Text = user["userPrincipalName"].ToString();

                    // just in case
                    btnSignInSignOut.Text = "Sign out";
                });
            }
            else
            {
                await DisplayAlert("Something went wrong with the API call", responseString, "Dismiss");
            }
        }
    }
}
