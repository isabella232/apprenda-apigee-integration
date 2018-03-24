# Setting up an API Proxy in Apigee for Authentication and Token Validation

Below you will find the steps and necessary code to set up an API Proxy in Apigee and configure this proxy to perform and enforce OAuth2 authentication. More importantly, this example shows you how given an access token you can request to get additional details like the email address of the authorized user this token represents.

The instructions are valid and tested with the version of Apigee as of March 2018.
Let's get started...

## Setup and Configuration
  * Create an API proxy in Apigee, link that proxy to an API product, a developer, and an app. Consult the Apigee documentation on the necessary steps
  * Visit the Apps portion of your Apigee Dashboard and retrieve the Consumer Key (mapped to client_id) and Consumer Secret (mapped to client_secret) corresponding to the App you created for this API proxy. Save them and you will use them later on
  * Go to the Develop tab of your API proxy and start configuring the API Proxy
  * First, create the Policies
  * The EchoMessage policy is used to add the apigee developer email address to the payload
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<AssignMessage name="EchoMessage">
    <DisplayName>EchoMessage</DisplayName>
    <AssignTo createNew="false" type="response"/>
    <Set>
        <Payload>{apigee.developer.email}</Payload>
    </Set>
    <IgnoreUnresolvedVariables>false</IgnoreUnresolvedVariables>
</AssignMessage>
```
  * The getaccesstoken policy is used to create a new bearer access token using the grant type provided
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<OAuthV2 async="false" continueOnError="false" enabled="true" name="getaccesstoken">
    <DisplayName>getaccesstoken</DisplayName>
    <Operation>GenerateAccessToken</Operation>
    <ExpiresIn>1800000</ExpiresIn>
    <!-- 30 minutes -->
    <SupportedGrantTypes>
        <!-- This part is very important: most real OAuth 2.0 apps will want to use other
         grant types. In this case it is important to NOT include the "client_credentials"
         type because it allows a client to get access to a token with no user authentication -->
        <GrantType>client_credentials</GrantType>
    </SupportedGrantTypes>
    <GrantType>request.queryparam.grant_type</GrantType>
    <GenerateResponse enabled="true"/>
    <Tokens/>
</OAuthV2>
```
  * The remove-header-authorization policy removes the authorization header from the API calls. This is optional
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<AssignMessage async="false" continueOnError="false" enabled="true" name="remove-header-authorization">
    <DisplayName>Remove Header Authorization</DisplayName>
    <Remove>
        <Headers>
            <Header name="Authorization"/>
        </Headers>
    </Remove>
    <IgnoreUnresolvedVariables>true</IgnoreUnresolvedVariables>
    <AssignTo createNew="false" transport="http" type="request"/>
</AssignMessage>
```
* The verify-oauth-v2-access-token policy validates the OAuth2 token provided in the headers and puts the proper properties in place so that the AssignMessage policy EchoMessage can return it back to the caller
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<OAuthV2 async="false" continueOnError="false" enabled="true" name="verify-oauth-v2-access-token">
    <DisplayName>Verify OAuth v2.0 Access Token</DisplayName>
    <Operation>VerifyAccessToken</Operation>
</OAuthV2>
```
  * Now that we created all the policies, its time to configure the API Proxy endpoints.
  * First, create the endpoint for authentication. This is the endpoint you will call to receive a bearer access token from apigee. Notice the basepath is `/auth` and the path to the actual flow is `/getaccesstoken` using a POST request. The other important part here is using the flow to call into the policy `getaccesstoken` created earlier
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<ProxyEndpoint name="AuthenticateProxy">
    <Description/>
    <FaultRules/>
    <Flows>
        <Flow name="getaccesstoken">
            <Description/>
            <Request>
                <Step>
                    <FaultRules/>
                    <Name>getaccesstoken</Name>
                </Step>
            </Request>
            <Response/>
            <Condition>(proxy.pathsuffix MatchesPath "/getaccesstoken") and (request.verb = "POST")</Condition>
        </Flow>
    </Flows>
    <HTTPProxyConnection>
        <BasePath>/auth</BasePath>
        <Properties/>
        <VirtualHost>secure</VirtualHost>
    </HTTPProxyConnection>
    <RouteRule name="NoRoute"/>
</ProxyEndpoint>
```
  * Now that we created the proxy to give us a token, it is time to create the proxy that will validate the token and allow us to use a token to retrieve details like the email address.
  * This endpoint is comprised of two parts. The PreFlow validation of the token provided using the policies `verify-oauth-v2-access-token` and `remove-header-authorization` and the Flow definition that sends a Response back using the policy `EchoMessage` defined earlier. Notice that the basepath is `/validate` and there are no other subpaths. It is important the BasePath here is different from other proxy endpoints. Also note that there is no Route defined. After this proxy endpoint is called, no redirection or routing to another endpoint needs to happen.
```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<ProxyEndpoint name="ValidateProxy">
    <Description/>
    <FaultRules/>
    <PreFlow name="PreFlow">
        <Request>
            <Step>
                <Name>verify-oauth-v2-access-token</Name>
            </Step>
            <Step>
                <Name>remove-header-authorization</Name>
            </Step>
        </Request>
        <Response/>
    </PreFlow>
    <Flows>
        <Flow name="getattributes">
            <Description/>
            <Request/>
            <Response>
                <Step>
                    <FaultRules/>
                    <Name>EchoMessage</Name>
                </Step>
            </Response>
        </Flow>
    </Flows>
    <HTTPProxyConnection>
        <BasePath>/validate</BasePath>
        <Properties/>
        <VirtualHost>secure</VirtualHost>
    </HTTPProxyConnection>
    <RouteRule name="NoRoute"/>
</ProxyEndpoint>
```
  * So far we defined the proxy endpoints. Go ahead and Publish your deployment to prod and test environments for your latest revision.

  ## Using the API Proxy
  * You can now use PowerShell or Curl or use any other HTTP API tool to invoke the APIs. I am using PowerShell.
  * Be aware that the base paths and other endpoint proxy paths in Apigee are `case sensitive`!
  * First, we make the request to get the bearer access token back. Substitute the client_id and client_secret with the ones you saved from earlier. Also change the initial part of the URI with the one defined for your Apigee Edge space. Notice i am using the `prod` deployment throughout the examples below.
```PowerShell
$result = Invoke-WebRequest -Uri https://<Your-space-identifier>-prod.apigee.net/auth/getaccesstoken?grant_type=client_credentials -Body "client_id=<insert client_id>&client_secret=<insert client secret>" -method Post
$result.Content | convertfrom-json | select access_token
```
  * The last command above will return the bearer access token if successful. You can use that token in the Headers below.
  * The command below will go against the validate API and return the email address that belongs to the token provided in the headers
```PowerShell
$Headers = @{}
$Headers["Authorization"] = "Bearer <insert bearer token after the space>"
$result = Invoke-WebRequest -Uri https://<Your-space-identifier>-prod.apigee.net/validate -method Post -Headers $Headers
$result.Content
```

Good luck with creating your own API proxies in Apigee! If you want to import the complete API proxy defined in this page, you can download it from  [ApigeeAuth_API_Proxy.zip](../attachments/ApigeeAuth_API_Proxy.zip)
