# Apprenda and Apigee Integration

Integrating the Apprenda Cloud Platform with the Apigee API management product to offer a unified API layer for all your APIs, including the ability to enforce OAuth2 in your endpoints.

## Documentation
- [Setting up an API Proxy in Apigee for Authentication and Token Validation](./docs/auth-proxy.md)
- [Setting up an API Proxy to create a facade in front of Web APIs implemented as applications on the Apprenda Cloud Platform](#setup-on-apigee)
- [Setting up the External Authentication Plugin on Apprenda](#add-authentication-plugin)

## Source
- The source code for the External Authentication Plugin in Apprenda

## Step-by-Step Installation and Setup

Apigee is an API management platform and the purpose of this integration is to access your web APIs hosted on Apprenda through Apigee securely with OAuth authorization. The steps below will explain what to do on both the Apprenda and Apigee end to get this integration working and what the end result will look like. There is also a [video walkthrough](linktovideo) of this process.


## Configure Your Apprenda App

To start you will need to have an Apprenda app that has a web API. For this tutorial we use the .NET template for web APIs, a very simple API that will return some pre-set values on call.

Upload your archive for your app to Apprenda to get started. For instructions on deploying apps to Apprenda please see [this guide](https://docs.apprenda.com/7-0/deployment-manifest) in the Apprenda docs.

When your archive is uploaded, go to the Configure tab and click on "User Access". You should see something similar to below:

![](images/tutorial_1.PNG)

When your user access model is set to "Anyone", as you might expect, anyone can make a request to your app. Below is a request made to our sample app with the access model set to "Anyone".


![](images/tutorial_2.PNG )


As expected we get our desired values back from our API call. Now if we were to set up a proxy through Apigee pointing to this endpoint in Apprenda, it would successfully make any call. 

If you are not worried about securing access of your API to certain people then this is all you need to have to use Apigee with your API, simply create an Apigee proxy pointing to your API and use the proxy to make all your requests. 

However if you would like to secure your API access with OAuth continue reading.


![](images/tutorial_3.PNG )


Go back to your Configure tab and change the User Access Model to "Authenticated". Note that if your app is already promoted you will need to demote it, change this setting, and promote again.

Now if we make the same call as above to our API, we can't get through because we are not authorized. Authorization is handled by a plugin to Apprenda so if no plugin is configured or if your call doesn't meet the requirements expected by the plugin you have provided, you will not be able to get through to your API.


![](images/tutorial_4.PNG )


## Add Authentication Plugin

Now in order to get our desired authenticated calls from Apigee we will need to upload the external authentication plugin. You can find both the [compiled archive](https://github.com/apprenda/apprenda-apigee-integration/blob/master/ExternalAuthentication.PlugIn/ExternalAuthPlugin.zip) and the source code for this plugin in the repo.

To upload the plugin, go to the SOC and under the "Security" tab, select "Service Authentication". You should see a page similar to below:


![](images/tutorial_5.PNG )


Click on "Enable External Authentication". A box will pop up for you to configure your plugin. First select the file provided in the repo for the external auth plugin, found [here](https://github.com/apprenda/apprenda-apigee-integration/blob/master/ExternalAuthentication.PlugIn/ExternalAuthPlugin.zip). Now in the HTTP-HEADERS text box you must enter a comma separated list of the headers which are expected by the plugin. In our case these are "Authorization" and "ApigeeHost" as these are the headers which the Apigee proxy you will configure next will supply when you make an API call through it. 

![](images/tutorial_6.PNG )

Click upload and wait for your plugin to successfully upload.

### Explanation of Plugin

The plugin will look for these specific headers when a request is received for any app with an access level set to Authorization. The plugin login takes the information in these two headers (the Authorization header contains the access key, and the ApigeeHost header contains the url of your Apigee environment) and use them to verify the validity of your request. The authorization header is supplied by you when you make a call through the Apigee proxy, the ApigeeHost header is added on automatically by your proxy.

The first check it will make is to verify that the ApigeeHost provided exists in the registry setting whitelist for external authentication (this registry setting will be created later on in the tutorial). If that URL is in the whitelist, it will make a request to your proxy's verification endpoint and provide the access token it received. Your proxy will then validate the access token it receives from the plugin, and if the token is valid it will return the email address of the Apigee developer associated with that token. As long as this email address is the email of an Apprenda tenant who has access to the app you are requesting, your request will be completed and Apprenda will execute your desired API call. Otherwise, the request is denied and you will not get through to your API.

![diagram](images/ApigeeIntegration.PNG)

The above diagram shows the interaction between the user, Apigee, the plugin, and your Apprenda app.

Now that you have an external authentication plugin, you will need to configure a proxy on Apigee that supplies the information that your plugin is looking for.


## Setup on Apigee

First you will want to upload the provided "ApprendaProxy.zip" to Apigee. You can find this in the repo in the "Apigee Proxies" folder, or directly [here](https://github.com/apprenda/apprenda-apigee-integration/blob/master/Apigee%20Proxies/ApprendaProxy.zip). From your Apigee dashboard, go to "API Proxies" and in the top right click on the "+ Proxy" button.

In the proxy creation menu, select the last option, "Proxy bundle", and hit Next.


![](images/tutorial_7.PNG )


On the next screen, select your proxy file and give the proxy any name you wish. Click next again.


![](images/tutorial_8.PNG )


On the next screen simply click "Build" and once your proxy has finished building, follow the link to view your proxy in the proxy editor.


### Proxy Overview

Your proxy has two Proxy Endpoints and one Target Endpoint. Keep in mind when making your calls to your API that Apigee base paths are case sensitive.

The first proxy endpoint, "MyAppEndpoint" points to the Target Endpoint "MyAppTarget", which is the URL of your Apprenda app's API and is accessed with the base path "myapp". This endpoint contains the logic to provide your plugin with an access token and the proxy's URL in order for the plugin to make a call back to the proxy to verify the access token. Note that you can change the name of this endpoint and the base path to whatever you want, as long as you update your API calls to use this new name.

The second proxy endpoint, "VerifyEndpoint" has no target endpoint and will never need to be called by you. This endpoint is called by the Apprenda External Authentication Plugin to verify the access token provided using the URL provided by your proxy when you make a call through your "MyAppEndpoint". The base path of this endpoint is expected by the plugin so you must **not** change the base path without updating the source code of the plugin to reflect that change. Also note that you should not deploy any additional proxies with this base path (base paths are shared across an organization on Apigee so you cannot re-use any base paths of active proxies.)

This proxy is configured to access a single API, if you would like to add additional apps please see the instructions [here](#adding-additional-apis).


![](images/tutorial_9.PNG )


Now to configure your proxy, click on the "Develop" tab in the top right. You should see the screen below:


![](images/tutorial_10.PNG )


If you would like to change the name or the base path for your app's endpoint you may do so here but it is not necessary for this tutorial.

Now click on "MyAppTarget" at the bottom of the navigator on the left. Here you will need to update the URL to your desired URL for your API. See the section highlighted below.



![](images/tutorial_11.PNG )



### Deploy Proxy 

Once you have everything configured as you like you can deploy your proxy. In the top banner of your proxy's page click on the "Deployment" button and select an environment to deploy it to. For this tutorial we will be deploying to "test". If you deploy to "prod" instead all your steps will be the same you will just need to make sure to use the URL for the prod environment rather than test.


![](images/tutorial_12.PNG )


After you deploy, when viewing the Overview tab you should be able to see a list of all URLs endpoints for your proxy. 



![](images/tutorial_13.PNG )


Either URL for the "myapp" endpoint is the URL you will want to save to use for your API call later on. In this case our URL is "[https://jolinger-eval-test.apigee.net/myapp](https://jolinger-eval-test.apigee.net/myapp)". You will never directly call the "verify" endpoint, this is done by the plugin.


### Developers

The next thing you will need to do is create a developer in Apigee. The developer you make must have the same email address as a tenant in Apprenda which is authorized to use your app.

To create a developer, select "Publish" and then "Developers" from the left menu and then click on the "+ Developer" button in the top right. Select a First Name, Last Name, Username and Email for your developer. Only the email needs to match your Apprenda tenant account.



![](images/tutorial_14.PNG )



### Product

The next step is to create a product and associate it with your API. Go to "Publish" on the left menu bar and then "API Products". Click on the "+ API Product" button in the top right.

Give your product any name you wish, then select the environments you would like to use, or select both. Set the access level to public and key approval to automatic. Under resources select the proxy you created. Add resource paths for '/' and '/**'. See the screenshot below.


![](images/tutorial_15.PNG )


Click save on the bottom right and your product is ready to use.


### App

The final step on the Apigee side is to create an app. Go back to Publish and then to Apps. Click on the "+ App" button in the top right. Give the app any name you want. For developer, select the developer you created earlier. Remember the email address needs to be the same as your Apprenda tenant. 

Under credentials, you can set the expiration of the credentials. In this example the credentials never expire. Also under credentials, select the product you just created. You can see the settings I used in the screenshot below.


![](images/tutorial_16.PNG )


Click save on the bottom right. You will be taken to the Apps page, click on the app you just created to get your credentials.


### Create OAuth Proxy

You can use an OAuth proxy in Apigee to validate your access token. This proxy is provided by Apigee and so it may already be on your account, called "oauth". If not, it is provided in the Apprenda repo in the "Apigee Proxies" folder, or directly [here](https://github.com/apprenda/apprenda-apigee-integration/blob/master/Apigee%20Proxies/oauth.zip). This proxy does not have any target endpoints and is not attached to any app or product, all it does is generate an access token from your client credentials.

Upload the OAuth proxy (if you don't already have it). Your proxy should look like this: 

![](images/tutorial_17.PNG )


Make sure you deploy this proxy to either prod or test if it is not deployed already.

You will use the path "/oauth/client_credential/accesstoken" to generate an access token, providing your Consumer Key and Consumer Secret, which you will do below. If you would like to refresh the access token after creation there is also the "/refresh_accesstoken" path.

If you would like to change the duration of your access token you can do so in the Develop tab, in the "GenerateAccessTokenClient" under Policies. The location is highlighted below. The expiration is in milliseconds.


![](images/tutorial_18.PNG )



### Generate Access Token

The Consumer Key and Consumer Secret are on the overview page for the app you created and are what you will need to generate your access token and start using your API. Click on the "Show" button next to each and save these somewhere.


![](images/tutorial_19.PNG )


Now we are ready to generate the access token. Go to your console or whatever your preferred platform is for making your API calls.

We make the following call to generate our access token using the path for the OAuth proxy explained above. If you have changed the base path for this proxy, or you are using the prod environment rather than test, then adjust your call accordingly:

`curl -X POST -H "Content-Type: application/x-www-form-urlencoded" https://jolinger-eval-test.apigee.net/oauth/client_credential/accesstoken?grant_type=client_credentials -d 'client_id=KEY&client_secret=SECRET`

You should get a response that looks something like this:


![](images/tutorial_20.PNG )



### Add Your Apigee URL to Authentication Whitelist

Now before you can make a call to your API there is actually one last step in Apprenda.

For security reasons you will want to whitelist any URL that you are using to connect to this app, the plugin will check the whitelist when it receives a request to verify that the source is legitimate, before it verifies the access token provided. 

Go back to the Apprenda SOC and then go to "Platform Registry" under "Configuration" and click the button to add a registry setting.

![](images/tutorial_21.PNG )


The name of the setting needs to be Authentication.ExternalServicesWhitelist, and the value is a comma separated list of all URLs you want to allow. In this case we only have one which is the one we saved earlier.

![](images/tutorial_22.PNG )

You should not include the path to your endpoint in this, just the base URL. As you add additional endpoints you do not need to change this setting. If you are going to switch to the prod environment you would need to add the URL for that (which in this example would be https://jolinger-eval-prod.apigee.net).

### Make your API Call

Now we are finally ready to make a call to your target API. Get the URL you saved earlier from the proxy you created. Ours is "[https://jolinger-eval-test.apigee.net/myapp](https://jolinger-eval-test.apigee.net/myapp)". Using the access token you just generated, make a call to this URL, add your desired API call, and add a header for authorization with the access token as the value. In our example this call looks like this:

`curl https://jolinger-eval-test.apigee.net/myapp/api/values -H "Authorization: Bearer zS4lRlDNIkXBL9Aq9TxQK7ahBnmp"`


![](images/tutorial_23.PNG )


And as you can see we get a result from our API, the same result we got in the beginning with no authorization enabled. Now if you attempt the same call and leave out the header or provide an invalid access token, you will not be able to get through. As shown here:


![](images/tutorial_24.PNG )


And here:


![](images/tutorial_25.PNG )


#### Congratulations, you have secured your Apprenda app with OAuth access through Apigee!


## Adding Additional APIs

If you have more than one web API contained in your Apprenda apps you would like to access through Apigee, you do not need to create additional proxies, simply add another target endpoint for each additional API.

![](images/tutorial_26.PNG )

Now add another proxy endpoint, and copy and paste the contents of your existing endpoint ("myapp" in our example). Change the name of the endpoint, and give it your desired base path and then change the target endpoint to the name one you just created above. In the example our second target endpoint is named "MyAppTarget2", our second proxy endpoint is named "MyAppEndpoint2", and the base path is "myapp2".

![](images/tutorial_27.PNG )

You may have to save as a new revision and deploy the proxy again. Now you can use this endpoint in the exact same way. You can add as many of these as you like just make sure you don't re-use names for the base path.

## Known Limitations

*   If there are multiple tenants with the same email address in Apprenda and Apigee forwards a call to an API with said email address attached to the access token, the call will only be successful if the first tenant found is the one attached to the desired app.
*   Base paths for proxy endpoints in Apigee are case sensitive
*   Proxy endpoints in Apigee are organization-wide so you cannot have multiple endpoints with the same base path. 
