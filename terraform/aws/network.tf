
resource "aws_vpc" "vpc" {
  tags = {
    Name      = "unad-vpc"
    managedBy = "terraform"
  }
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true
}

resource "aws_vpc_endpoint" "secrets_manager" {
  vpc_id            = aws_vpc.vpc.id
  vpc_endpoint_type = "Interface"
  service_name      = "com.amazonaws.${data.aws_region.current.name}.secretsmanager"
  subnet_ids        = aws_subnet.private_subnet.*.id
}

resource "aws_vpc_endpoint" "ssm" {
  vpc_id            = aws_vpc.vpc.id
  vpc_endpoint_type = "Interface"
  service_name      = "com.amazonaws.${data.aws_region.current.name}.ssm"
  subnet_ids        = aws_subnet.private_subnet.*.id
}

resource "aws_eip" "nat_eip" {
  count = length(local.availability_zones)

  domain = "vpc"

  tags = {
    Name = "nat-eip-${count.index}"
  }
}

resource "aws_nat_gateway" "nat_gateway" {
  count = length(local.availability_zones)

  allocation_id = aws_eip.nat_eip[count.index].id
  subnet_id     = aws_subnet.public_subnet[count.index].id

  tags = {
    Name = "nat-gateway-${count.index}"
  }
}

resource "aws_route_table" "private_route_table" {
  count = length(local.availability_zones)

  vpc_id = aws_vpc.vpc.id

  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.nat_gateway[count.index].id
  }

  tags = {
    Name = "private-route-table-${count.index}"
  }
}

resource "aws_route_table_association" "private_route_table_association" {
  count          = length(local.availability_zones)
  subnet_id      = aws_subnet.private_subnet[count.index].id
  route_table_id = aws_route_table.private_route_table[count.index].id
}

resource "aws_subnet" "private_subnet" {
  count             = length(local.availability_zones)
  vpc_id            = aws_vpc.vpc.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index + 1)
  availability_zone = local.availability_zones[count.index]

  tags = {
    Name = "private-subnet-${count.index}"
  }

  map_public_ip_on_launch = false
}

resource "aws_subnet" "public_subnet" {
  count             = length(local.availability_zones)
  vpc_id            = aws_vpc.vpc.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, count.index + 11)
  availability_zone = local.availability_zones[count.index]

  tags = {
    Name = "public-subnet-${count.index}"
  }

  map_public_ip_on_launch = true
}

resource "aws_internet_gateway" "igw" {
  vpc_id = aws_vpc.vpc.id

  tags = {
    Name = "unad-igw"
  }
}

resource "aws_route_table" "public_route_table" {
  vpc_id = aws_vpc.vpc.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.igw.id
  }

  tags = {
    Name = "public-route-table"
  }
}

resource "aws_route_table_association" "public_route_table_association" {
  count          = length(local.availability_zones)
  subnet_id      = aws_subnet.public_subnet[count.index].id
  route_table_id = aws_route_table.public_route_table.id
}

