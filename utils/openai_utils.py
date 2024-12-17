from openai import OpenAI, AuthenticationError
import os
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

def get_openai_client():
    """Get OpenAI client with API key from environment"""
    # Ensure environment variables are loaded
    load_dotenv()
    
    api_key = os.getenv('OPENAI_API_KEY')
    # Print masked version of API key
    masked_key = f"{api_key[:8]}...{api_key[-4:]}" if api_key else "None"
    print(f"Using API key: {masked_key}")
    
    if not api_key:
        raise ValueError("OPENAI_API_KEY not found in environment variables")
    
    try:
        client = OpenAI(api_key=api_key)
        # Test the API key with a simple request
        client.models.list()
        return client
    except AuthenticationError as e:
        raise ValueError(f"Invalid API key: {str(e)}")
    except Exception as e:
        raise ValueError(f"Error initializing OpenAI client: {str(e)}")

def wait_on_run(client, run, thread_id):
    """Wait for the assistant run to complete"""
    try:
        while run.status == 'queued' or run.status == 'in_progress':
            run = client.beta.threads.runs.retrieve(
                thread_id=thread_id,
                run_id=run.id
            )
            time.sleep(0.5)  # Small delay to avoid excessive API calls
        return run
    except Exception as e:
        raise Exception(f"Error while waiting for assistant run: {str(e)}")

def get_assistant_response(client, thread_id):
    """Get the assistant's response messages"""
    try:
        messages = client.beta.threads.messages.list(thread_id=thread_id)
        for msg in messages.data:
            if msg.role == "assistant":
                return msg.content[0].text.value
        return None
    except Exception as e:
        raise Exception(f"Error getting assistant response: {str(e)}")

def mask_string(text, visible_chars_ends=10):
    """Mask sensitive data showing first and last N characters"""
    if not text:
        return None
    if len(text) <= visible_chars_ends * 2:
        return text
    return text[:visible_chars_ends] + '*' * (len(text) - (visible_chars_ends * 2)) + text[-visible_chars_ends:]
