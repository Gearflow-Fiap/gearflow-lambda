output "api_endpoint" {
  description = "URL base do API Gateway"
  value       = aws_apigatewayv2_api.auth.api_endpoint
}

output "validate_cpf_url" {
  value = "${aws_apigatewayv2_api.auth.api_endpoint}/auth/validate-cpf"
}

output "check_client_url" {
  value = "${aws_apigatewayv2_api.auth.api_endpoint}/auth/check-client"
}

output "generate_token_url" {
  value = "${aws_apigatewayv2_api.auth.api_endpoint}/auth/generate-token"
}

output "lambda_function_names" {
  value = {
    validate_cpf   = aws_lambda_function.validate_cpf.function_name
    check_client   = aws_lambda_function.check_client.function_name
    generate_token = aws_lambda_function.generate_token.function_name
  }
}

output "check_client_lambda_security_group_id" {
  description = "ID do security group da Lambda check-client. Alimenta allowed_security_group_ids em gearflow-infra-database para liberar só esta Lambda no RDS."
  value       = aws_security_group.check_client_lambda.id
}
