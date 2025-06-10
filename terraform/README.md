# Things Not Created by Terraform

- Secrets Manager Secret at `/unad/ec2/jumpbox/ssh-keypair`.  A JSON object that contains an ed_25519 key pair for SSH access to the jumpbox.  This must be generated locally and set in Secrets Manager manually per-environment.  Should be generated with `ssh-keygen -t ed25519 -C "jumpbox"`
- DNS Nameservers for hosted zones must be set after running `terraform apply` and creating the zone `aws_route53_zone.main`
