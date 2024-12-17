import os
import time
from dotenv import load_dotenv
from utils.openai_utils import get_openai_client, wait_on_run, get_assistant_response, mask_string
from utils.terminal_utils import (
    print_header, print_success, print_info, print_warning, print_error, color_text
)

def read_file_content(file_path):
    """Read content of a file"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception as e:
        print(f"Error reading file {file_path}: {str(e)}")
        return None

def get_code_review(client, assistant_id, file_content, file_path):
    """Get code review from OpenAI assistant"""
    try:
        # Create a thread
        thread = client.beta.threads.create()

        # Add message to thread
        message = client.beta.threads.messages.create(
            thread_id=thread.id,
            role="user",
            content=f"Please review this code:\n\n```{os.path.splitext(file_path)[1]}\n{file_content}\n```"
        )

        # Run the assistant
        run = client.beta.threads.runs.create(
            thread_id=thread.id,
            assistant_id=assistant_id
        )

        # Wait for completion
        while True:
            run_status = client.beta.threads.runs.retrieve(
                thread_id=thread.id,
                run_id=run.id
            )
            if run_status.status == 'completed':
                break
            elif run_status.status == 'failed':
                raise Exception("Assistant run failed")
            time.sleep(1)

        # Get the assistant's response
        messages = client.beta.threads.messages.list(
            thread_id=thread.id
        )
        
        # Return the latest assistant message
        for msg in messages.data:
            if msg.role == "assistant":
                return msg.content[0].text.value

        return None

    except Exception as e:
        print(f"Error getting code review for {file_path}: {str(e)}")
        return None

def main():
    load_dotenv()
    
    api_key = os.getenv('OPENAI_API_KEY')
    assistant_id = os.getenv('OPENAI_ASSISTANT_ID')
    
    print_header("\nDebug Environment Variables:")
    print("-" * 50)
    print_info(f"API Key found: {'Yes' if api_key else 'No'}")
    print_info(f"API Key: {mask_string(api_key) if api_key else 'None'}")
    print_info(f"Assistant ID found: {'Yes' if assistant_id else 'No'}")
    print_info(f"Assistant ID: {mask_string(assistant_id) if assistant_id else 'None'}")
    print("-" * 50)
    print()
    
    try:
        client = get_openai_client()
        assistant_id = os.getenv('OPENAI_ASSISTANT_ID')
        
        print_info(f"Using assistant ID: {mask_string(assistant_id)}")
        
        if not assistant_id:
            print_error("Error: OPENAI_ASSISTANT_ID not found in .env file")
            return
        
        # Validate assistant ID
        try:
            client.beta.assistants.retrieve(assistant_id)
        except Exception as e:
            print_error(f"Error: Invalid assistant ID - {str(e)}")
            return

        # Get current directory
        current_dir = os.path.dirname(os.path.abspath(__file__))
        
        # Read files-structure.txt
        structure_file = os.path.join(current_dir, 'files-structure.txt')
        if not os.path.exists(structure_file):
            print("Error: files-structure.txt not found. Please run setup.py first.")
            return
        
        # Read the file list
        with open(structure_file, 'r', encoding='utf-8') as f:
            files = f.read().splitlines()
        
        # Create reviews directory if it doesn't exist
        reviews_dir = os.path.join(current_dir, 'reviews')
        os.makedirs(reviews_dir, exist_ok=True)
        
        # Process each file
        for file_path in files:
            print_header(f"\nProcessing {file_path}...")
            
            # Get full path to source file
            full_path = os.path.join(current_dir, 'input', file_path)
            
            # Read file content
            content = read_file_content(full_path)
            if not content:
                print_error(f"Failed to read {file_path}")
                continue
            
            # Get code review
            review = get_code_review(client, assistant_id, content, file_path)
            if not review:
                print_error(f"Failed to get review for {file_path}")
                continue
            
            # Create the same directory structure in reviews folder
            review_dir = os.path.join(reviews_dir, os.path.dirname(file_path))
            os.makedirs(review_dir, exist_ok=True)
            
            # Save review to file maintaining the original path structure
            review_file = os.path.join(
                reviews_dir,
                f"{os.path.splitext(file_path)[0]}_review.md"
            )
            try:
                with open(review_file, 'w', encoding='utf-8') as f:
                    f.write(f"# Code Review for {file_path}\n\n{review}")
                print_success(f"Review saved to {review_file}")
            except Exception as e:
                print_error(f"Error saving review for {file_path}: {str(e)}")

    except ValueError as e:
        print_error(f"Configuration error: {str(e)}")
        print_warning("Please check your .env file and make sure OPENAI_API_KEY is set correctly")
    except Exception as e:
        print_error(f"Unexpected error: {str(e)}")

if __name__ == "__main__":
    main()
