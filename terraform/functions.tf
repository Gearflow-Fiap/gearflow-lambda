locals {
  name_prefix = "${var.project_name}-${var.environment}"

  lambda_env_common = {
    ASPNETCORE_ENVIRONMENT = var.environment
  }
}

# AWS Academy Lab não permite criar IAM Roles — usa a LabRole pré-existente
data "aws_iam_role" "lab_role" {
  name = "LabRole"
}

# --- validate-cpf ---

resource "aws_lambda_function" "validate_cpf" {
  function_name    = "${local.name_prefix}-validate-cpf"
  role             = data.aws_iam_role.lab_role.arn
  handler          = "GearFlow.Lambda.ValidateCpf::GearFlow.Lambda.ValidateCpf.Function::FunctionHandler"
  runtime          = "dotnet8"
  filename         = var.validate_cpf_zip_path
  source_code_hash = filebase64sha256(var.validate_cpf_zip_path)
  memory_size      = 256
  timeout          = 30

  environment {
    variables = local.lambda_env_common
  }
}

# --- check-client ---

resource "aws_lambda_function" "check_client" {
  function_name    = "${local.name_prefix}-check-client"
  role             = data.aws_iam_role.lab_role.arn
  handler          = "GearFlow.Lambda.CheckClient::GearFlow.Lambda.CheckClient.Function::FunctionHandler"
  runtime          = "dotnet8"
  filename         = var.check_client_zip_path
  source_code_hash = filebase64sha256(var.check_client_zip_path)
  memory_size      = 256
  timeout          = 30

  environment {
    variables = merge(local.lambda_env_common, {
      DB_CONNECTION_STRING = var.db_connection_string
    })
  }
}

# --- generate-token ---

resource "aws_lambda_function" "generate_token" {
  function_name    = "${local.name_prefix}-generate-token"
  role             = data.aws_iam_role.lab_role.arn
  handler          = "GearFlow.Lambda.GenerateToken::GearFlow.Lambda.GenerateToken.Function::FunctionHandler"
  runtime          = "dotnet8"
  filename         = var.generate_token_zip_path
  source_code_hash = filebase64sha256(var.generate_token_zip_path)
  memory_size      = 256
  timeout          = 30

  environment {
    variables = merge(local.lambda_env_common, {
      JWT_SIGNING_KEY          = var.jwt_signing_key
      JWT_ISSUER               = var.jwt_issuer
      JWT_AUDIENCE             = var.jwt_audience
      JWT_ACCESS_TOKEN_MINUTES = tostring(var.jwt_access_token_minutes)
    })
  }
}

# --- API Gateway HTTP API ---

resource "aws_apigatewayv2_api" "auth" {
  name          = "${local.name_prefix}-auth-api"
  protocol_type = "HTTP"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.auth.id
  name        = "$default"
  auto_deploy = true
}

resource "aws_apigatewayv2_integration" "validate_cpf" {
  api_id                 = aws_apigatewayv2_api.auth.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.validate_cpf.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_integration" "check_client" {
  api_id                 = aws_apigatewayv2_api.auth.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.check_client.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_integration" "generate_token" {
  api_id                 = aws_apigatewayv2_api.auth.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.generate_token.invoke_arn
  payload_format_version = "2.0"
}

resource "aws_apigatewayv2_route" "validate_cpf" {
  api_id    = aws_apigatewayv2_api.auth.id
  route_key = "POST /auth/validate-cpf"
  target    = "integrations/${aws_apigatewayv2_integration.validate_cpf.id}"
}

resource "aws_apigatewayv2_route" "check_client" {
  api_id    = aws_apigatewayv2_api.auth.id
  route_key = "POST /auth/check-client"
  target    = "integrations/${aws_apigatewayv2_integration.check_client.id}"
}

resource "aws_apigatewayv2_route" "generate_token" {
  api_id    = aws_apigatewayv2_api.auth.id
  route_key = "POST /auth/generate-token"
  target    = "integrations/${aws_apigatewayv2_integration.generate_token.id}"
}

resource "aws_lambda_permission" "validate_cpf_apigw" {
  statement_id  = "AllowAPIGatewayInvokeValidateCpf"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.validate_cpf.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.auth.execution_arn}/*/*"
}

resource "aws_lambda_permission" "check_client_apigw" {
  statement_id  = "AllowAPIGatewayInvokeCheckClient"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.check_client.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.auth.execution_arn}/*/*"
}

resource "aws_lambda_permission" "generate_token_apigw" {
  statement_id  = "AllowAPIGatewayInvokeGenerateToken"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.generate_token.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.auth.execution_arn}/*/*"
}
