using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWebFormsComponents
{
	public class ScheduledJsRuntime : IJSRuntime {

		private readonly IJSRuntime _JSRuntime;
		private readonly Queue<Task> _Queue = new Queue<Task>();

		public ScheduledJsRuntime(IJSRuntime jSRuntime)
		{
			_JSRuntime = jSRuntime;
		}

		public IJSRuntime InnerRuntime => _JSRuntime;

		public bool AfterRender { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
		{

			if (AfterRender) return _JSRuntime.InvokeAsync<TValue>(identifier, args);

			var theTask = new Task<TValue>( () => _JSRuntime.InvokeAsync<TValue>(identifier, args).GetAwaiter().GetResult());
			_Queue.Enqueue(theTask);

			return new ValueTask<TValue>(theTask);

		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
		{

			if (AfterRender) return _JSRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);

			var theTask = new Task<TValue>(() => _JSRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args).GetAwaiter().GetResult());
			_Queue.Enqueue(theTask);

			return new ValueTask<TValue>(theTask);

		}

		public Task SetAfterRender(bool value) {

			AfterRender = value;
			if (value) return RunTasks();

			return Task.CompletedTask;

		}

		public Task RunTasks() {

			if (!_Queue.Any()) return Task.CompletedTask;

			var theTasks = new List<Task>();

			while (!_Queue.Any()) {

				var t = _Queue.Dequeue();
				t.Start();
				theTasks.Add(t);

			}

			return Task.WhenAll(theTasks.ToArray());

		}

	}

}
