# PlayFlow Guide 
* Install PlayFlow to deploy the testing server from the link. https://assetstore.unity.com/packages/tools/network/playflow-cloud-206903


### Setup
* PlayFlow -> PlayFlow Coud window
* Paste the Token fromt he playflow account
* Publish the build
* Start the Server
* Paste the IP from the logs into the NetworkManager Client Address.

--------------------------------------------------------------------------

# AWS Guide
### EC2 Instance Setup
* Create a new isntance of EC2 using Ubuntu.
* Download and save the ppm key to a safe place
* Select the new Instance from the list
  * Go to Security tab
  * Click the security link
  * Click on Edit Inboud rules on the right side.
  * Add rule - Custom TCP and paste the post, Add another rule - Custom UDP and paste the port
  * Pass the 0.0.0.0/0 in both the rules and click Save Rules.
* Select the new Instance from the list and click connect. Go to SSH Client Tab and copy the command given below to connect to your instance using its Public DNS.
  > It will look something like this: ec2-PUBLIC-IP-Goes-Here.ap-south-1.compute.amazonaws.com
* Make a Server build for Linux in Unity - Need Linux module installed, Change platform to Deverloper Server in Build Settings. 
* Open Terminal in the folder where the ppm key is placed and paste the copied command to connect to the server. Click Yes if asked. Once connected, will see the ubuntu user name.
* Open a separate terminal and transfer the server build to the server using this command scp -i "YOUR EC2 PEM FILE PATH" -r "BUILD LOCATION ON YOUR LOCAL MACHINE" user@ip:
  > It will look something like this: scp -i "YOUR EC2 PEM FILE" -r "BUILD LOCATION  ON YOUR LOCAL MACHINE" ubuntu@ec2-PUBLIC-IP-GOES-HERE.ap-south-1.compute.amazonaws.com
* Go to ubuntu terminal, change directory to build folder using:
cd /BUILD FOLDER PATH
* Provide the permission to the executable file using this command: 
  > chmod +x /PATH OF EXECUTABLE FILE
* Run this command to run the build:
  > ./PATH OF EXECUTABLE FILE




### S3 Bucket Setup
* Create a new bucket.
* Paste the pubic url and same port in the NetworkManager class in the Unity Editor and Make a WebGL build. Compressed build might not work.
* Upload the build to the bucket.
* Select the bucket and go to permission tab and set the followings
  * Block public access -> DISABLE
  * Object Ownership -> BUCKET OWNER PRESERRED (ACL ENABLED)
  * Paste the following code in the Bucket Policy
    > {
    >    "Version": "2012-10-17",
    >    "Id": "PolicyForPublicWebsiteContent",
    >    "Statement": [
    >        {
    >            "Sid": "PublicReadGetObject",
    >            "Effect": "Allow",
    >            "Principal": {
    >                "AWS": "*"
    >            },
    >            "Action": "s3:GetObject",
    >            "Resource": "arn:aws:s3:::farzicafedemo/*"
    >        }
    >    ]
    >}
* Go to Proerties Tab and set the followings:
  * Bucket Versionaing -> DISABLED
  * Default Encryption -> DISABLED
  * Server Access Logging -> DISABLED
  * Amazon EventBridge -> OFF
  * Transfer acceleration -> DISABLED
  * Object Lock - DISABLED
  * Request Pays -> DISABLED
  
* Select bucket, go to Peroperties tab and scroll down to the bottom of the page to see the website URL. 
