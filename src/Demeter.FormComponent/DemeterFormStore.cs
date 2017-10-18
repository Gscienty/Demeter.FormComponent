using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Nest;

namespace Demeter.FormComponent
{
    public sealed class DemeterFormStore<TForm> : IFormStore<TForm>
        where TForm : DemeterForm, new()
    {
        private readonly IMongoCollection<TForm> _formCollection;
        private readonly ElasticClient _elasticClient;

        public DemeterFormStore(
            IMongoDatabase database,
            string formsCollection,
            ElasticClient elasticClient = null)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }
            if (formsCollection == null)
            {
                throw new ArgumentNullException(nameof(formsCollection));
            }

            this._formCollection = database.GetCollection<TForm>(formsCollection);
            this._elasticClient = elasticClient;
        }

        async Task<FormResult> IFormStore<TForm>.CreateAsync(TForm form,
            CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }
            await Task.WhenAll(
                this._formCollection.InsertOneAsync(
                    form,
                    cancellationToken: cancellationToken
                ),
                Task.Run(async () =>
                {
                    if (this._elasticClient == null)
                    {
                        return;
                    }

                    await this._elasticClient.IndexAsync(
                        form, m => m.Id(form.Id)
                    );
                })
            );

            return FormResult.Success;
        }

        async Task<FormResult> IFormStore<TForm>.DeleteAsync(TForm form,
            CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            cancellationToken.ThrowIfCancellationRequested();
            form.Delete();

            var query = Builders<TForm>.Filter.Eq(f => f.Id, form.Id);
            var update = Builders<TForm>
                .Update
                .Set(f => f.DeleteOn, form.DeleteOn);

            var result = (await Task.WhenAll(
                this._formCollection.UpdateOneAsync(
                    query,
                    update,
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken
                ),
                Task.Run(async () =>
                {
                    if (this._elasticClient == null)
                    {
                        return (UpdateResult)UpdateResult.Unacknowledged.Instance;
                    }

                    var response = await this._elasticClient.DeleteAsync<TForm>(
                        DocumentPath<TForm>.Id(form.Id)
                    );

                    return (UpdateResult)UpdateResult.Unacknowledged.Instance;
                })
            ))[0];
            return result.IsModifiedCountAvailable && result.ModifiedCount == 1
                ? FormResult.Success
                : FormResult.Failed();
        }

        async Task<TForm> IFormStore<TForm>.FindByIdAsync(string id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var query = Builders<TForm>.Filter.And(
                Builders<TForm>.Filter.Eq(f => f.Id, id),
                Builders<TForm>.Filter.Eq(f => f.DeleteOn, null)
            );

            return await this._formCollection.Find(query).FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        async Task<TNewForm> IFormStore<TForm>.QueryAsync<TNewForm>(
            string queryString,
            int count,
            Func<IQueryable<TForm>, TNewForm> queryAction,
            CancellationToken cancellationToken)
        {
            if (queryString == null)
            {
                throw new ArgumentNullException(nameof(queryString));
            }
            if (this._elasticClient == null)
            {
                throw new NullReferenceException(nameof(this._elasticClient));
            }

            var response = await this._elasticClient.SearchAsync<TForm>(s => s
                .From(0).Size(count).Query(q =>
                q.QueryString(m => m.Query(queryString))
            ));
            return queryAction(response.Hits
                .Select(hit => DemeterForm.QueryHitTransfer(hit))
                .AsQueryable()
            );
        }

        async Task<FormResult> IFormStore<TForm>.UpdateAsync(TForm form, CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            var query = Builders<TForm>.Filter.And(
                Builders<TForm>.Filter.Eq(f => f.DeleteOn, null),
                Builders<TForm>.Filter.Eq(f => f.Id, form.Id)
            );
            
            var result = (await Task.WhenAll(
                this._formCollection.ReplaceOneAsync(
                    query,
                    form,
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken
                ),
                Task.Run(async () => 
                {
                    if (this._elasticClient == null)
                    {
                        return (ReplaceOneResult)ReplaceOneResult.Unacknowledged.Instance;
                    }

                    await this._elasticClient.UpdateAsync<TForm>(
                        DocumentPath<TForm>.Id(form.Id),
                        update => update.Doc(form)
                    );

                    return (ReplaceOneResult)ReplaceOneResult.Unacknowledged.Instance;
                })
            ))[0];

            return result.IsModifiedCountAvailable && result.MatchedCount == 1
                ? FormResult.Success
                : FormResult.Failed();
        }

        Task<TNewForm> IFormStore<TForm>.QueryAsync<TNewForm>(
            Func<IQueryable<TForm>, TNewForm> queryAction,
            CancellationToken cancellationToken) => Task.FromResult(
                queryAction(this._formCollection
                    .AsQueryable()
                    .Where(f => f.DeleteOn == null)
                )
            );

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DemeterFormStore() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void System.IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}