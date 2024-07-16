# This request tells the lambda function to run the database initialization code, it also send a url which 
# will be used by the function for future requests
POST https://bso2z4suvb.execute-api.us-east-1.amazonaws.com/initialize
{"url": "https://bso2z4suvb.execute-api.us-east-1.amazonaws.com/"}