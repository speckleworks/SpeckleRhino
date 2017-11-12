<template>
  <v-dialog fullscreen v-model='visible' style='width: 100%' >
    <v-card>
      <v-card-title>
        Add Receiver
      </v-card-title>        
      <v-card-text class="grey darken-4">
        <div class='step-1'>
          <div class='subheading'>
            Select an account:
          </div>
          <v-select v-bind:items='selectItems' v-model='selectedAccountValue' style='z-index: 9000'></v-select>
          <br>
            <div class='subheading' v-show='selectedAccountValue!=null && !fail'>
            Existing streams:
            <v-select v-bind:items='streamsMap' v-model='selectedStream' style='z-index: 9000'autocomplete :search-input.sync="streamsMap"></v-select>
            Or input a stream id:
            <v-text-field v-model='directStreamId'></v-text-field>
          </div>
          <v-alert color='error' :value='fail' icon='error'>
            Failed to contact server.
            <br>
            {{ error }}
          </v-alert>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn flat @click.native="visible=false">Cancel</v-btn>
        <v-btn color='light-blue' flat @click.native='addReceiver'>Add</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import API from '../store/apicaller.js'
import { EventBus } from '../event-bus'

export default {
  name: 'ClientReceiverAdd',
  computed: {
    accounts() { 
      return this.$store.getters.accounts
    },
    selectItems() {
      return this.$store.getters.accounts.map( a => a.serverName + this.separator + a.email )
    },
    streamsMap(){
      return this.streams.map( s => s.name + this.separator + s.streamId )
    }
  },
  watch: {
    selectedAccountValue( value ) {
      if( !value ) return
      let acDet = value.split( this.separator )
      this.selectedAccount = this.accounts.find( ac => ac.serverName === acDet[0] && ac.email === acDet[1])
      API.getStreams( this.selectedAccount )
      .then( res => {
        this.fail = false
        console.log(res)
        this.streams = res.streams
      })
      .catch( err => {
        this.streams = []
        this.fail = true
        this.error = err.toString()
      })
    },
    visible( value ) {
      if( value ) return
      this.selectedAccountValue = null
      this.selectedAccount = null
      this.selectedStream = null
      this.streams = []
    }
  },
  data() {
    return {
      visible: false,
      separator: ' | ',
      deleteDialog: false,
      selectedAccountValue: null,
      selectedAccount: null,
      selectedStream: null,
      directStreamId: null,
      streams:[],
      fail: false,
      error: null
    }
  },
  methods: {
    addReceiver() {
      let streamId = null
      if( this.selectedStream ) streamId = this.selectedStream.split( this.separator )[ 1 ]
      else if( this.directStreamId ) streamId = this.directStreamId
      if( !streamId ) {
        this.fail = true
        this.error = 'No streamid provided.'
        return
      }
      let payload = { 
        account: this.selectedAccount,
        streamId: streamId
      }
      Interop.addReceiverClient( JSON.stringify( payload ) )
      this.visible = false
    }
  }, 
  mounted () {
   EventBus.$on('show-add-receiver-dialog', () => {
      this.visible = true
    }) 
  }
}
</script>

<style lang="scss">
.list__tile__title {
  font-size: 12px !important;
}
</style>