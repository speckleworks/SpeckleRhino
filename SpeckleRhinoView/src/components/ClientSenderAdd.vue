<template>
  <v-dialog fullscreen transition='dialog-bottom-transition' v-model='visible' style='width: 100%'>
    <v-card>
      <v-toolbar style="flex: 0 0 auto;" dark class='light-blue'>
        <v-btn icon @click.native="visible = false" dark>
          <v-icon>close</v-icon>
        </v-btn>
        <v-toolbar-title>Add Sender</v-toolbar-title>
      </v-toolbar>
      <v-card-text text-center>
        <div class='step-1'>
          <v-form>
            <v-select label="Account" required v-bind:items='userAccounts' v-model='selectedAccountValue' style='z-index: 9000' autocomplete :search-input:sync='userAccounts'></v-select>
            <v-text-field label="Stream name" v-model="streamName" required></v-text-field>
          </v-form>
          <v-card v-if='objectSelection.length > 0' class='elevation-4 pa-2'>
            <div class='pa-2'>Stream will be created based on your object selection:</div>
            <template v-for='sel in objectSelection'>
              <div class='caption'>
                <v-chip xxxsmall class='eliptic' color='' style='text-align: left;'>
                  <v-avatar :style='{ backgroundColor: sel.color }'>{{sel.objectCount}}</v-avatar>
                  {{sel.layerName}}
                </v-chip>
              </div>
            </template>
          </v-card>
          <v-card v-else class='elevation-4 pa-2'>
            <div class='pa-2'>No selection found. Please select some objects!</div>
            <!-- <div class='caption pa-1'>
              <template v-for='layer, index in layerSelectionMap'>
                <v-checkbox small v-model='layer.selected' :label='layer.layerName + " | " + layer.objectCount + " objs"' class='layer-selector' color='grey lighten-4'></v-checkbox>
              </template>
            </div> -->
          </v-card>
          <v-alert color='error' :value='fail' icon='error'>
            {{ error }}
          </v-alert>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn flat @click.native="visible=false">Cancel</v-btn>
        <v-btn color='light-blue' @click.native='addSender'>Add</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
<script>
import API from '../store/apicaller.js'
import { EventBus } from '../event-bus'

export default {
  name: 'ClientSenderAdd',
  computed: {
    accounts( ) { return this.$store.getters.accounts },
    userAccounts( ) { return this.$store.getters.accounts.map( a => a.serverName + ', ' + a.email ) },
    objectSelection( ) { return this.$store.getters.selection },
    layerInfo( ) { return this.$store.getters.layerInfo },
  },
  watch: {
    selectedAccountValue( value ) {
      if ( !value ) return
      this.selectedAccount = this.accounts.find( ac => { return ac.serverName === value.split(', ')[0] && ac.email === value.split(', ')[1] } )
      API.getStreams( this.selectedAccount )
        .then( res => {
          this.fail = false
        } )
        .catch( err => {
          this.fail = true
          this.error = err.toString( )
        } )
    },
    visible( value ) {
      if ( value ) {
        Interop.getLayersAndObjectsInfo( true )
          .then( res => {
            this.$store.commit( 'SET_LAYERINFO', JSON.parse( res ) )
            this.layerSelectionMap = this.layerInfo.map( layer => { return { selected: false, layerName: layer.layerName, objectCount: layer.objectCount } } )
          } )
        return
      }
      this.selectedAccountValue = null
      this.selectedAccount = null
    }
  },
  data( ) {
    return {
      visible: false,
      selectedAccountValue: null,
      selectedAccount: null,
      fail: false,
      error: null,
      streamName: null,
      layerSelectionMap: [ ],
      layerSelection: null
    }
  },
  methods: {
    addSender( ) {
      if ( this.selectedAccount == null ) {
        this.error = 'Please select an account.'
        this.fail = true
        return
      }
      if ( !this.streamName || /^\s*$/.test( this.streamName ) ) {
        this.error = 'Please input a stream name.'
        this.fail = true
        return
      }
      if ( this.objectSelection.length == 0 ) {
        this.error = 'Please select some objects.'
        this.fail = true
        return
      }

      let payload = {
        account: this.selectedAccount,
        streamName: this.streamName,
        selection: this.objectSelection.reduce( ( a, l ) => { return [ ...a, ...l.objectGuids ] }, [ ] )
      }

      Interop.addSenderClientFromSelection( JSON.stringify( payload ) )
      this.visible = false
    }
  },
  mounted( ) {
    EventBus.$on( 'show-add-sender-dialog', ( ) => {
      this.visible = true
    } )
  }
}
</script>
<style lang="scss">
.layer-selector label {
  font-size: 12px !important;
}

.layer-selector .input-group__details {
  display: none;
}

.eliptic {
  max-width: 100%;
  text-overflow: ellipsis;
  white-space: nowrap;
  overflow: hidden;
}

.list__tile__title,
.input-group__selections__comma {
  white-space: nowrap;
} // .input-group__details {
//   display: none;
// }
</style>