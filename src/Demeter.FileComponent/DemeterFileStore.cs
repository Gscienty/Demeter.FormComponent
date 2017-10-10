using System;
using System.Collections.Generic;
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
        private readonly IMongoCollection<TFile> _formCollection;
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
            this._formCollection = database.GetCollection<TFile>(formsCollection);
            this._baseFolderPath = folderPath;

            this._elasticClient = elasticClient;
        }

        async Task<FormResult> IFormStore<TFile>.CreateAsync(TFile file, CancellationToken cancellationToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Mutex writeMutex = new Mutex(true);
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    writeMutex.WaitOne();
                    await DemeterFileUtil.WriteAsync(this._baseFolderPath, file.Id, file.Content);
                    writeMutex.ReleaseMutex();
                }),
                this._formCollection
                .InsertOneAsync(file, cancellationToken: cancellationToken),
                Task.Run(async () =>
                {
                    if (this._elasticClient == null)
                    {
                        return ;
                    }
                    byte[] tempStore = file.Content;
                    writeMutex.WaitOne();
                    file.Content = null;
                    await this._elasticClient.IndexAsync(file, m => m.Id(file.Id));
                    file.Content = tempStore;
                    writeMutex.ReleaseMutex();
                })
            ).ConfigureAwait(false);

            writeMutex.Dispose();

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
                this._formCollection.UpdateOneAsync(
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
                    var formResult = await this._formCollection.Find(query)
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

       async Task<IEnumerable<TFile>> IFormStore<TFile>.LastestAsync(int count, CancellationToken cancellationToken)
        {
            var query = Builders<TFile>.Filter.Eq(f => f.DeleteOn, null);

            return await this._formCollection
                .Find(query)
                .SortByDescending(f => f.CreateOn)
                .Limit(count)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        async Task<IEnumerable<TFile>> IFormStore<TFile>.QueryAsync(string queryString, int count, CancellationToken cancellationToken)
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

            return response.Documents;
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

            Mutex writeMutex = new Mutex(true);
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    writeMutex.WaitOne();
                    await DemeterFileUtil.WriteAsync(this._baseFolderPath, file.Id, file.Content);
                    writeMutex.ReleaseMutex();
                }),
                this._formCollection
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

                    byte[] tempStore = file.Content;

                    writeMutex.WaitOne();
                    file.Content = null;
                    await this._elasticClient.UpdateAsync<TFile>(
                        DocumentPath<TFile>.Id(file.Id),
                        update => update.Doc(file)
                    );
                    file.Content = tempStore;
                    writeMutex.ReleaseMutex();

                    return (ReplaceOneResult)ReplaceOneResult.Unacknowledged.Instance;
                })
            ).ConfigureAwait(false);

            writeMutex.Dispose();

            return FormResult.Success;
        }

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