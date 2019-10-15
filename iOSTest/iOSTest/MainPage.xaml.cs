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
            // let's see if we have a user in our belly already
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
                        .WithUseEmbeddedWebView(true)
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
