import os
import asyncio
from semantic_kernel import Kernel
from semantic_kernel.agents import ChatCompletionAgent, ChatHistoryAgentThread
from semantic_kernel.connectors.ai.open_ai import OpenAIChatCompletion, OpenAIPromptExecutionSettings
from semantic_kernel.connectors.ai import FunctionChoiceBehavior
from semantic_kernel.functions import KernelArguments

class Agent:

    def __init__(self):
        settings = OpenAIPromptExecutionSettings()
        settings.function_choice_behavior = FunctionChoiceBehavior.Auto()
        
        self.kernel = Kernel()
        self.kernel.add_service(OpenAIChatCompletion(
            api_key=os.getenv("OPENAI_API_KEY"),
            ai_model_id=os.getenv("OPENAI_MODEL")
        ))
        
        self.agent = ChatCompletionAgent(
            kernel=self.kernel,
            arguments=KernelArguments(),
        )     
        self.thread : ChatHistoryAgentThread | None = None
    
    def run_conversation_loop(self):
        async def conversation():
            thread : ChatHistoryAgentThread | None = None  
            while True:
                input_text = input("User > ")
                print("Assistant > ",end="")
                async for response in self.agent.invoke_stream(messages=input_text, thread=thread):
                    print(response, end="")
                    thread = response.thread
                print(end="\n\n")        
                     
        asyncio.run(conversation())
    
    async def ask_streaming(self, message: str):
        async for response in self.agent.invoke_stream(messages=message, thread=self.thread):
            self.thread = response.thread
            yield response.content.content