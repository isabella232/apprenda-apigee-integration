

<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<AssignMessage name="EchoMessage">
    <DisplayName>EchoMessage</DisplayName>
    <AssignTo createNew="false" type="response"/>
    <Set>
        <Payload>{apigee.developer.email}</Payload>
    </Set>
    <IgnoreUnresolvedVariables>false</IgnoreUnresolvedVariables>
</AssignMessage>

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

<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<OAuthV2 async="false" continueOnError="false" enabled="true" name="verify-oauth-v2-access-token">
    <DisplayName>Verify OAuth v2.0 Access Token</DisplayName>
    <Operation>VerifyAccessToken</Operation>
</OAuthV2>

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


! case sensitive!!!

$result = Invoke-WebRequest -Uri https://jolinger-eval-prod.apigee.net/auth/getaccesstoken?grant_type=client_credentials -Body "client_id=jKzlafT6LejrMZrTgCVWxuZUndprpyce&client_secret=Cj4t1TY9vkpzEzp1" -method Post
$result.Content | convertfrom-json | select access_token

$Headers = @{}
$Headers["Authorization"] = "Bearer vJHWFviWAwGOTxYgzNgZ1Pzx7dLV"
$result = Invoke-WebRequest -Uri https://jolinger-eval-prod.apigee.net/validate -method Post -Headers $Headers
$result.Content
