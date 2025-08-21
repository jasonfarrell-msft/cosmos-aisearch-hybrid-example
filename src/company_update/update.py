#!/usr/bin/env python3
"""
Script to read from Cosmos DB, find company information, and update Azure Search index.
Captures respondentid, surveyid, question, and company name, then updates the search index.
"""

import os
import hashlib
import uuid
from dotenv import load_dotenv
from azure.cosmos import CosmosClient, exceptions
from azure.search.documents import SearchClient
from azure.core.credentials import AzureKeyCredential

def get_deterministic_id(respondent_id, survey_id, question):
    """
    Generate a deterministic GUID based on respondent ID, survey ID, and question.
    This mimics the C# logic from the SurveyData class.
    """
    if not respondent_id or not survey_id:
        raise ValueError("Missing RespondentId or SurveyId - cannot create EntryId")
    
    # Create question hash (mimicking C# GetHashCode behavior approximation)
    question_hash = str(hash(question) if question else 0)
    
    # Combine the IDs and question hash to create a unique string
    combined_string = f"{respondent_id}_{survey_id}_{question_hash}"
    
    # Use SHA256 to create a deterministic hash of the combined string
    sha256_hash = hashlib.sha256(combined_string.encode('utf-8')).digest()
    
    # Convert the first 16 bytes to a GUID format
    guid_bytes = sha256_hash[:16]
    
    # Create UUID from bytes
    return str(uuid.UUID(bytes=guid_bytes))

def main():
    # Load environment variables from .env file
    load_dotenv()
    
    # Get Cosmos DB configuration from environment
    endpoint = os.environ['COSMOS_ENDPOINT']
    key = os.environ['COSMOS_KEY']
    database_name = os.environ['COSMOS_DATABASE']
    container_name = os.environ['COSMOS_CONTAINER']

    # Azure Search configuration
    search_endpoint = os.environ["SEARCH_ENDPOINT"]
    search_key = os.environ["SEARCH_KEY"]
    search_index_name = os.environ["SEARCH_INDEX_NAME"]

    if not all([endpoint, key, database_name, container_name]):
        print("Error: Missing required Cosmos DB environment variables. Please check your .env file.")
        return
    
    try:
        # Initialize Cosmos client
        client = CosmosClient(endpoint, key)
        database = client.get_database_client(database_name)
        container = database.get_container_client(container_name)
        
        # Initialize Azure Search client
        search_client = SearchClient(
            endpoint=search_endpoint,
            index_name=search_index_name,
            credential=AzureKeyCredential(search_key)
        )
        
        print(f"Connected to Cosmos DB: {database_name}/{container_name}")
        print(f"Connected to Azure Search: {search_index_name}")
        print("Processing documents and updating search index...\n")
        
        # Query all documents in the container
        query = "SELECT * FROM c"
        documents = container.query_items(query=query, enable_cross_partition_query=True)
        
        document_count = 0
        updates_made = 0
        
        for document in documents:
            document_count += 1
            
            # Extract required fields
            respondent_id = document.get('Respondent ID')
            survey_id = document.get('Collector ID')
            question = document.get('Question')
            
            if not respondent_id or not survey_id:
                print(f"Skipping document {document_count}: Missing Respondent ID or Collector ID")
                continue
            
            # Search for company information in Column fields from 11 to 33
            company = None
            for column_num in range(11, 34):  # 11 to 33 inclusive
                column_key = f"Column{column_num}"
                column_value = document.get(column_key)
                
                # Check if the value exists and is not null/empty
                if column_value is not None and str(column_value).strip():
                    company = column_value
                    break
            
            if company:
                try:
                    # Generate the deterministic ID using the C# logic
                    entry_id = get_deterministic_id(respondent_id, survey_id, question)
                    
                    print(f"Document {document_count}:")
                    print(f"  RespondentId: {respondent_id}")
                    print(f"  SurveyId: {survey_id}")
                    print(f"  Question: {question}")
                    print(f"  Company: {company}")
                    print(f"  Generated ID: {entry_id}")
                    
                    # Update the search index entry
                    update_document = {
                        "id": entry_id,
                        "Company": company
                    }
                    
                    # Merge the update (this will update existing fields or create new document)
                    result = search_client.merge_or_upload_documents([update_document])
                    
                    if result[0].succeeded:
                        print(f"  ✓ Successfully updated search index")
                        updates_made += 1
                    else:
                        print(f"  ✗ Failed to update search index: {result[0].error_message}")
                    
                except Exception as e:
                    print(f"  ✗ Error processing document: {e}")
                
                print("-" * 70)
            else:
                print(f"Document {document_count}: No company found in Column11-Column33")
        
        print(f"\nSummary:")
        print(f"  Total documents processed: {document_count}")
        print(f"  Search index updates made: {updates_made}")
        
    except exceptions.CosmosHttpResponseError as e:
        print(f"Cosmos DB error: {e}")
    except Exception as e:
        print(f"Unexpected error: {e}")

if __name__ == "__main__":
    main()
