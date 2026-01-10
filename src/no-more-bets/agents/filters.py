"""Semantic Kernel Filters for logging and monitoring plugin usage."""

import os
from typing import Awaitable, Callable
from semantic_kernel.filters import FunctionInvocationContext

async def plugin_usage_logger_filter(
    context: FunctionInvocationContext,
    next: Callable[[FunctionInvocationContext], Awaitable[None]]
) -> None:
    """Filter that logs plugin function invocations and their results.
    
    Logs the plugin function name and result in a formatted output.
    
    Parameters
    ----------
    context : FunctionInvocationContext
        The context containing function and execution information.
    next : Callable
        The next filter or function in the pipeline.
    """
    
    log_calls = os.getenv("LOG_AGENT_FUNCTION_CALLS", "false").lower() == "true"
    
    if log_calls:
        plugin_name = context.function.plugin_name
        function_name = context.function.name   
        arguments = context.arguments
        print(f"Calling: {plugin_name}: {function_name} with arguments: {arguments}")

    await next(context)
