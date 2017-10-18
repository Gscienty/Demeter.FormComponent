using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Demeter.FormComponent;
using MongoDB.Driver;
using Nest;

namespace Demeter.FileComponent
{
    public class DemeterFileStore<TFile> : IFormStore<TFile>
        where TFile : DemeterFile, new()
    {
        private readonly IMongoCollection<TFile> _fileCollection;
        private readonly ElasticClient _elasticClient;
        private readonly string _baseFolderPath;
        
        public DemeterFileStore(
            IMongoDatabase database,
            string formsCollection,
            string folderPath,
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
            this._fileCollection = database.GetCollection<TFile>(formsCollection);
            this._baseFolderPath = folderPath;

            this._elasticClient = elasticClient;
        }

        async Task<FormResult> IFormStore<TFile>.CreateAsync(TFile file, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            byte[] tempStore = file.Content;
            file.Content = null;
            await Task.WhenAll(
                DemeterFileUtil.WriteAsync(this._baseFolderPath, file.Id, tempStore),
                this._fileCollection
                .InsertOneAsync(file, cancellationToken: cancellationToken),
                Task.Run(async () =>
                {
                    if (this._elasticClient == null)
                    {
                        return ;
                    }
                    await this._elasticClient.IndexAsync(file, m => m.Id(file.Id));
                })
            ).ConfigureAwait(false);
            file.Content = tempStore;

            return FormResult.Success;
        }

        async Task<FormResult> IFormStore<TFile>.DeleteAsync(TFile file, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            file.Delete();

            var query = Builders<TFile>.Filter.And(
                Builders<TFile>.Filter.Eq(f => f.Id, file.Id),
                Builders<TFile>.Filter.Eq(f => f.DeleteOn, null)
            );
            var update = Builders<TFile>.Update.Set(f => f.DeleteOn, file.DeleteOn);

            var result = (await Task.WhenAll(
                this._fileCollection.UpdateOneAsync(
                    query,
                    update,
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken
                ),
                Task.Run(async () =>
                {
                    await DemeterFileUtil.DeleteAsync(this._baseFolderPath, file.Id);
                    return UpdateResult.Unacknowledged.Instance as UpdateResult;
                }),
                Task.Run(async () => {
                    if (this._elasticClient == null)
                    {
                        return UpdateResult.Unacknowledged.Instance as UpdateResult;
                    }
                    
                    await this._elasticClient.DeleteAsync<TFile>(
                        DocumentPath<TFile>.Id(file.Id)
                    );

                    return UpdateResult.Unacknowledged.Instance as UpdateResult;
                })
            ).ConfigureAwait(false))[0];

            return result.IsAcknowledged && result.ModifiedCount == 1
                ? FormResult.Success
                : FormResult.Failed(new FormError { Code = "404", Description = "not find file"});
        }

        async Task<TFile> IFormStore<TFile>.FindByIdAsync(string id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var query = Builders<TFile>.Filter.And(
                Builders<TFile>.Filter.Eq(f => f.Id, id),
                Builders<TFile>.Filter.Eq(f => f.DeleteOn, null)
            );

            var result = await Task.WhenAll(
                Task.Run(async () =>
                {
                    var formResult = await this._fileCollection.Find(query)
                        .FirstOrDefaultAsync(cancellationToken);
                    return formResult as DemeterFile;
                }),
                Task.Run(async () =>
                {
                    return new DemeterFile(id)
                    {
                        Content = await DemeterFileUtil.ReadAsync(this._baseFolderPath, id)
                    };
                })
            ).ConfigureAwait(false);

            if (result[0] == null)
            {
                return null;
            }
            else 
            {
                result[0].Content = result[1].Content;
                return result[0] as TFile;
            }
        }
        
        async Task<TNewFile> IFormStore<TFile>.QueryAsync<TNewFile>(
            string queryString,
            int count,
            Func<IQueryable<TFile>, TNewFile> queryAction,
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

            var response = await this._elasticClient.SearchAsync<TFile>(s => s
                .From(0).Size(count).Query(q =>
                q.QueryString(m => m.Query(queryString))
            ));

            return queryAction(response.Hits
                .Select(hit => DemeterForm.QueryHitTransfer(hit))
                .AsQueryable()
            );
        }

        async Task<FormResult> IFormStore<TFile>.UpdateAsync(TFile file, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var query = Builders<TFile>.Filter.And(
                Builders<TFile>.Filter.Eq(f => f.Id, file.Id),
                Builders<TFile>.Filter.Eq(f => f.DeleteOn, null)
            );

            byte[] tempStore = file.Content;
            file.Content = null;
            await Task.WhenAll(
                DemeterFileUtil.WriteAsync(this._baseFolderPath, file.Id, tempStore),
                this._fileCollection
                    .ReplaceOneAsync(
                        query,
                        file,
                        new UpdateOptions { IsUpsert = false },
                        cancellationToken
                ),
                Task.Run(async () =>
                {
                    if (this._elasticClient == null)
                    {
                        return (ReplaceOneResult)ReplaceOneResult.Unacknowledged.Instance;
                    }
                    await this._elasticClient.UpdateAsync<TFile>(
                        DocumentPath<TFile>.Id(file.Id),
                        update => update.Doc(file)
                    );

                    return (ReplaceOneResult)ReplaceOneResult.Unacknowledged.Instance;
                })
            ).ConfigureAwait(false);
            file.Content = tempStore;

            return FormResult.Success;
        }

        Task<TNewFile> IFormStore<TFile>.QueryAsync<TNewFile>(
            Func<IQueryable<TFile>, TNewFile> queryAction,
            CancellationToken cancellationToken) => Task.FromResult(
                queryAction(this._fileCollection
                    .AsQueryable()
                    .Where(f => f.DeleteOn == null)
                )
            );

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
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
        // ~DemeterFileStore() {
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