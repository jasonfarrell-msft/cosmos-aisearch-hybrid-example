
import logging
import json
import azure.functions as func

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')
    
    try:
        # Get JSON body from the request
        req_body = req.get_json()
        
        if not req_body:
            return func.HttpResponse(
                "Request body must contain JSON",
                status_code=400
            )
        
        # Extract the query field
        query = req_body.get('query')
        
        # Validate query field exists and is not empty
        if not query or (isinstance(query, str) and query.strip() == ""):
            return func.HttpResponse(
                "Missing or empty 'query' field in request body",
                status_code=400
            )
        
        # Return success with the query value
        response_data = {
            "query": query,
            "message": "Query received successfully"
        }
        
        return func.HttpResponse(
            json.dumps(response_data),
            status_code=200,
            headers={"Content-Type": "application/json"}
        )
        
    except ValueError:
        return func.HttpResponse(
            "Invalid JSON in request body",
            status_code=400
        )
    except Exception as e:
        logging.error(f"Error processing request: {str(e)}")
        return func.HttpResponse(
            "Internal server error",
            status_code=500
        )