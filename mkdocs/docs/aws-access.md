# AWS Account Access

Some developers will need access to the AWS Console for many tasks in this system.  In order to create a new development user, [follow these steps](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_users_create.html).

!!! danger "Security Tip"
    Ensure that every user has the **minimum required access** to resources.

## AWS CLI Setup

Once a developer can successfully access the AWS Console, they must now generate [Access Keys](https://docs.aws.amazon.com/powershell/latest/userguide/pstools-appendix-sign-up.html), and install the [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html).

Once installed, it can be configured by running `aws configure` and entering the necessary values.

To verify the configuration, simply run `aws ec2 describe-instances`.  If no errors are returned, no further action is required.


