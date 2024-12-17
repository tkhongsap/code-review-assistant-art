import os
from pathlib import Path
from dotenv import load_dotenv
from openai import OpenAI
import time

# Load environment variables
load_dotenv()

def read_review_file(file_path):
    """Read content of a review file"""
    with open(file_path, 'r', encoding='utf-8') as f:
        return f.read()

def wait_for_run_completion(client, thread_id, run_id):
    """Wait for the assistant run to complete"""
    while True:
        run = client.beta.threads.runs.retrieve(thread_id=thread_id, run_id=run_id)
        if run.status == "completed":
            return run
        elif run.status == "failed":
            raise Exception("Assistant run failed")
        time.sleep(1)

def generate_directory_summary(client, assistant_id, directory_path, reviews):
    """Generate a summary for a specific directory"""
    # Create a new thread
    thread = client.beta.threads.create()
    
    # Combine all reviews into one message
    combined_reviews = "\n\n".join([f"# {path}\n{content}" for path, content in reviews.items()])
    
    # Create message in thread
    message = client.beta.threads.messages.create(
        thread_id=thread.id,
        role="user",
        content=f"""Please analyze these code reviews for the directory {directory_path} and create a comprehensive summary with:

1. Directory Overview - A high-level summary of this directory's purpose and components
2. Key Findings - Main points and patterns from the reviews
3. Recommendations - Suggested improvements based on the reviews

Reviews to analyze:

{combined_reviews}"""
    )
    
    # Run the assistant
    run = client.beta.threads.runs.create(
        thread_id=thread.id,
        assistant_id=assistant_id
    )
    
    # Wait for completion
    run = wait_for_run_completion(client, thread.id, run.id)
    
    # Get the response
    messages = client.beta.threads.messages.list(thread_id=thread.id)
    return messages.data[0].content[0].text.value

def get_directory_structure(file_structure_path):
    """Parse the file structure and return a dictionary of directories and their files"""
    directories = {}
    with open(file_structure_path, 'r', encoding='utf-8') as f:
        for line in f:
            path = line.strip()
            if path:
                dir_path = os.path.dirname(path)
                if dir_path not in directories:
                    directories[dir_path] = []
                directories[dir_path].append(os.path.basename(path))
    return directories

def generate_master_summary(client, assistant_id, directory_summaries):
    """Generate a master summary of all directory summaries"""
    # Create a new thread
    thread = client.beta.threads.create()
    
    # Combine all directory summaries
    combined_summaries = "\n\n".join([f"# {dir_path}\n{summary}" for dir_path, summary in directory_summaries.items()])
    
    # Create message in thread
    message = client.beta.threads.messages.create(
        thread_id=thread.id,
        role="user",
        content=f"""Please analyze these directory summaries and create a comprehensive project-level summary with:

1. Project Overview - A high-level overview of the project's architecture and components
2. Key Findings - Major patterns, strengths, and areas of concern across the codebase
3. Critical Recommendations - Priority improvements that would benefit the entire project
4. Component Analysis - Brief analysis of major components and their interactions
5. Technical Debt - Assessment of technical debt and maintenance concerns

Directory Summaries to analyze:

{combined_summaries}"""
    )
    
    # Run the assistant
    run = client.beta.threads.runs.create(
        thread_id=thread.id,
        assistant_id=assistant_id
    )
    
    # Wait for completion
    run = wait_for_run_completion(client, thread.id, run.id)
    
    # Get the response
    messages = client.beta.threads.messages.list(thread_id=thread.id)
    return messages.data[0].content[0].text.value

def save_summary(base_dir, directory_path, summary, is_master=False):
    """Save summary with descriptive filename"""
    # Create the directory structure in reviews-summary
    summary_dir = os.path.join(base_dir, directory_path) if not is_master else base_dir
    os.makedirs(summary_dir, exist_ok=True)
    
    # Create descriptive filename
    if is_master:
        filename = f"{os.path.basename(directory_path)}_project_summary.md"
    else:
        dir_name = os.path.basename(directory_path) or directory_path.replace('\\', '_').replace('/', '_')
        filename = f"{dir_name}_summary.md"
    
    # Save the summary
    summary_path = os.path.join(summary_dir, filename)
    with open(summary_path, 'w', encoding='utf-8') as f:
        f.write(summary)
    
    return summary_path

def generate_summary_report():
    """Generate a hierarchical summary report using OpenAI assistant"""
    # Initialize OpenAI client
    client = OpenAI()
    assistant_id = os.getenv('OPENAI_CODE_REVIEW_SUMMARY_ASSISTANT_ID')
    
    # Get directory structure
    directories = get_directory_structure('files-structure.txt')
    
    # Create reviews-summary directory if it doesn't exist
    summary_base_dir = 'reviews-summary'
    os.makedirs(summary_base_dir, exist_ok=True)
    
    # Store all directory summaries
    directory_summaries = {}
    
    # Process directories from deepest to shallowest
    sorted_dirs = sorted(directories.keys(), key=lambda x: len(x.split('\\')), reverse=True)
    
    for directory in sorted_dirs:
        # Get all review files in this directory
        review_files = {}
        reviews_dir = Path('reviews')
        for review_file in reviews_dir.rglob(f"{directory}/*_review.md"):
            review_files[str(review_file)] = read_review_file(review_file)
        
        if review_files:
            # Generate summary for this directory
            summary = generate_directory_summary(client, assistant_id, directory, review_files)
            directory_summaries[directory] = summary
            
            # Save summary with descriptive filename
            summary_path = save_summary(summary_base_dir, directory, summary)
            print(f"Generated summary for: {directory} -> {summary_path}")
    
    # Generate master summary if we have directory summaries
    if directory_summaries:
        master_summary = generate_master_summary(client, assistant_id, directory_summaries)
        
        # Get the root project directory (first part of the path)
        root_project_dir = sorted_dirs[-1].split('\\')[0]
        
        # Save the master summary with project name
        summary_path = save_summary(summary_base_dir, root_project_dir, master_summary, is_master=True)
        print(f"Generated master project summary: {summary_path}")

if __name__ == "__main__":
    generate_summary_report()
