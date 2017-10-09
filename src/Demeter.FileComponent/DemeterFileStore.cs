using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Demeter.FormComponent;
using MongoDB.Driver;

namespace Demeter.FileComponent
{
    public class DemeterFileStore<TFile> : IFormStore<TFile>
        where TFile : DemeterFile, new()
    {
        private readonly IMongoCollection<TFile> _formCollection;
        private readonly string _baseFolderPath;
        
        public DemeterFileStore(
            IMongoDatabase database,
            string formsCollection,
            string folderPath)
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
        }

        async Task<FormResult> IFormStore<TFile>.CreateAsync(TFile form, CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            await Task.WhenAll(
                DemeterFileUtil.WriteAsync(this._baseFolderPath, form.Id, form.Content),
                this._formCollection
                .InsertOneAsync(form, cancellationToken: cancellationToken)
            ).ConfigureAwait(false);

            return FormResult.Success;
        }

        async Task<FormResult> IFormStore<TFile>.DeleteAsync(TFile form, CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }
            form.Delete();

            var query = Builders<TFile>.Filter.And(
                Builders<TFile>.Filter.Eq(f => f.Id, form.Id),
                Builders<TFile>.Filter.Eq(f => f.DeleteOn, null)
            );
            var update = Builders<TFile>.Update.Set(f => f.DeleteOn, form.DeleteOn);

            var result = (await Task.WhenAll(
                this._formCollection.UpdateOneAsync(
                    query,
                    update,
                    new UpdateOptions { IsUpsert = false },
                    cancellationToken
                ),
                Task.Run(async () =>
                {
                    await DemeterFileUtil.DeleteAsync(this._baseFolderPath, form.Id);
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

        Task<IEnumerable<TFile>> IFormStore<TFile>.LastestAsync(int count, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        Task<IEnumerable<TFile>> IFormStore<TFile>.QueryAsync(string queryString, int count, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        async Task<FormResult> IFormStore<TFile>.UpdateAsync(TFile form, CancellationToken cancellationToken)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            var query = Builders<TFile>.Filter.And(
                Builders<TFile>.Filter.Eq(f => f.Id, form.Id),
                Builders<TFile>.Filter.Eq(f => f.DeleteOn, null)
            );

            await Task.WhenAll(
                DemeterFileUtil.WriteAsync(this._baseFolderPath, form.Id, form.Content),
                this._formCollection
                    .ReplaceOneAsync(
                        query,
                        form,
                        new UpdateOptions { IsUpsert = false },
                        cancellationToken
                )
            ).ConfigureAwait(false);

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