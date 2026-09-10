# Security group da Lambda check-client. Vive na mesma VPC privada do RDS
# (gerenciada pelo repo gearflow-infra-database — ver var.lambda_vpc_id) para
# poder alcançar o banco pela rede interna em vez de expô-lo publicamente.
resource "aws_security_group" "check_client_lambda" {
  name        = "${local.name_prefix}-check-client-lambda"
  description = "Egress da Lambda check-client para o SQL Server privado e para a internet via NAT"
  vpc_id      = var.lambda_vpc_id

  tags = {
    Name = "${local.name_prefix}-check-client-lambda"
  }
}

resource "aws_vpc_security_group_egress_rule" "check_client_sqlserver" {
  security_group_id            = aws_security_group.check_client_lambda.id
  referenced_security_group_id = var.database_security_group_id
  from_port                    = 1433
  to_port                      = 1433
  ip_protocol                  = "tcp"
  description                  = "Acesso ao SQL Server privado (customers.clients)"
}

# Necessário para CloudWatch Logs, Secrets Manager, etc. — a Lambda sai pelo
# NAT Gateway da VPC do banco (ver gearflow-infra-database/terraform/network.tf).
resource "aws_vpc_security_group_egress_rule" "check_client_https" {
  security_group_id = aws_security_group.check_client_lambda.id
  cidr_ipv4         = "0.0.0.0/0"
  from_port         = 443
  to_port           = 443
  ip_protocol       = "tcp"
  description       = "HTTPS para serviços AWS (CloudWatch Logs etc.) via NAT Gateway"
}
