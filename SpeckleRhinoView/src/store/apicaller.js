import Axios from 'axios'

export default {
  getStreams( account ) {
    return new Promise( ( resolve, reject ) => {
      if ( !account ) return reject( 'No account provided' )
      Axios.get( account.restApi + '/accounts/streams', { headers: { 'Authorization': account.apiToken } } )
        .then( res => {
          resolve( res.data )
        } )
        .catch( err => {
          reject( err )
        } )
    } )
  }

}